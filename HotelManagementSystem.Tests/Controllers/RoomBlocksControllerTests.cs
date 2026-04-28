using System.Security.Claims;
using AutoMapper;
using HotelManagementSystem.API.Controllers;
using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Enums;
using HotelManagementSystem.Shared.Interfaces;
using HotelManagementSystem.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelManagementSystem.Tests.Controllers
{
    /// <summary>
    /// Phase 12d — Room Blocking tests.
    ///
    /// Covered contracts:
    ///   1. GET /api/roomblocks          → 200 with list of all blocks (admin).
    ///   2. GET /api/roomblocks/{id}     → 200 for valid id; 404 for missing id.
    ///   3. POST /api/roomblocks         → 201 CreatedAtAction with the new block.
    ///   4. PUT /api/roomblocks/{id}     → 204 NoContent on success; 404 when missing.
    ///   5. DELETE /api/roomblocks/{id}  → 204 NoContent on success; 404 when missing.
    ///   6. POST /api/bookings           → 409 Conflict when a requested room is blocked
    ///                                      for overlapping dates.
    ///   7. POST /api/bookings           → 201 Created when block exists but dates don't overlap.
    ///   8. POST /api/bookings           → 201 Created when block is on a different room.
    /// </summary>
    public class RoomBlocksControllerTests
    {
        // =====================================================================
        //  Helpers
        // =====================================================================

        private static RoomBlocksController BuildBlocksController(
            Mock<IRepository<RoomBlock>> repo,
            Mock<IMapper> mapper,
            params string[] roles)
        {
            var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "1"));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            return new RoomBlocksController(repo.Object, mapper.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = principal }
                }
            };
        }

        private static BookingsController BuildBookingsController(
            Mock<IRepository<Booking>> bookingRepo,
            Mock<IRepository<Room>> roomRepo,
            Mock<IRepository<RoomBlock>> blockRepo,
            Mock<IMapper> mapper,
            int userId = 7,
            params string[] roles)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            return new BookingsController(bookingRepo.Object, roomRepo.Object, mapper.Object, blockRepo.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = principal }
                }
            };
        }

        // =====================================================================
        //  TEST 1 — GetAll returns 200 with all blocks
        // =====================================================================

        [Fact]
        public async Task GetAll_Returns200_WithBlockList()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            var data = new List<RoomBlock>
            {
                new() { Id = 1, RoomId = 10, Reason = "Pipes", BlockType = RoomBlockType.Maintenance },
                new() { Id = 2, RoomId = 11, Reason = "VIP",   BlockType = RoomBlockType.VIPHold    }
            };
            var dtos = data.Select(b => new RoomBlockDTO { Id = b.Id }).ToList();

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(data);
            mapper.Setup(m => m.Map<IEnumerable<RoomBlockDTO>>(data)).Returns(dtos);

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dtos, ok.Value);
        }

        // =====================================================================
        //  TEST 2a — GetById returns 200 for valid id
        // =====================================================================

        [Fact]
        public async Task GetById_Returns200_ForValidId()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            var block = new RoomBlock { Id = 5, RoomId = 10, Reason = "Renovation",
                                        BlockType = RoomBlockType.Renovation };
            var dto   = new RoomBlockDTO { Id = 5, RoomId = 10, RoomNumber = "R-10" };

            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(block);
            mapper.Setup(m => m.Map<RoomBlockDTO>(block)).Returns(dto);

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.GetById(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        // =====================================================================
        //  TEST 2b — GetById returns 404 for missing id
        // =====================================================================

        [Fact]
        public async Task GetById_Returns404_WhenNotFound()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RoomBlock?)null!);

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.GetById(99);

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  TEST 3 — Create returns 201 CreatedAtAction
        // =====================================================================

        [Fact]
        public async Task Create_Returns201_WithCreatedBlock()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            var dto    = new RoomBlockDTO { RoomId = 10, Reason = "Pipes",
                                            BlockType = RoomBlockType.Maintenance,
                                            StartDate = DateTime.Today,
                                            EndDate   = DateTime.Today.AddDays(3) };
            var entity = new RoomBlock { Id = 0, RoomId = 10 };
            var saved  = new RoomBlockDTO { Id = 1, RoomId = 10, RoomNumber = "R-10" };

            mapper.Setup(m => m.Map<RoomBlock>(dto)).Returns(entity);
            mapper.Setup(m => m.Map<RoomBlockDTO>(entity)).Returns(saved);
            repo.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(sut.GetById), created.ActionName);
            repo.Verify(r => r.AddAsync(entity), Times.Once);
        }

        // =====================================================================
        //  TEST 4a — Update returns 204 on success
        // =====================================================================

        [Fact]
        public async Task Update_Returns204_OnSuccess()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            var existing = new RoomBlock { Id = 5, RoomId = 10 };
            var dto      = new RoomBlockDTO { Id = 5, RoomId = 10, Reason = "Extended" };

            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
            repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.Update(5, dto);

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        // =====================================================================
        //  TEST 4b — Update returns 404 when block not found
        // =====================================================================

        [Fact]
        public async Task Update_Returns404_WhenNotFound()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RoomBlock?)null!);

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.Update(99, new RoomBlockDTO());

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  TEST 5a — Delete returns 204 on success
        // =====================================================================

        [Fact]
        public async Task Delete_Returns204_OnSuccess()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            repo.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.Delete(7);

            Assert.IsType<NoContentResult>(result);
            repo.Verify(r => r.DeleteAsync(7), Times.Once);
        }

        // =====================================================================
        //  TEST 5b — Delete returns 404 when block not found
        // =====================================================================

        [Fact]
        public async Task Delete_Returns404_WhenNotFound()
        {
            var repo   = new Mock<IRepository<RoomBlock>>();
            var mapper = new Mock<IMapper>();

            // Repository throws KeyNotFoundException when the id is missing
            repo.Setup(r => r.DeleteAsync(99)).ThrowsAsync(new KeyNotFoundException());

            var sut    = BuildBlocksController(repo, mapper, "Administrator");
            var result = await sut.Delete(99);

            Assert.IsType<NotFoundResult>(result);
        }

        // =====================================================================
        //  TEST 6 — BookingsController.Create → 409 when room is blocked
        // =====================================================================

        [Fact]
        public async Task CreateBooking_Returns409_WhenRoomIsBlocked()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var blockRepo   = new Mock<IRepository<RoomBlock>>();
            var mapper      = new Mock<IMapper>();

            var checkIn  = new DateTime(2026, 7, 10);
            var checkOut = new DateTime(2026, 7, 13);

            var dto = new BookingDTO
            {
                BookingType  = BookingType.NightStay,
                CheckInDate  = checkIn,
                CheckOutDate = checkOut,
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };

            roomRepo.Setup(r => r.GetByIdAsync(10))
                    .ReturnsAsync(new Room { Id = 10, RoomNumber = "R-10", PricePerNight = 100m });
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
            mapper.Setup(m => m.Map<Booking>(dto))
                  .Returns(new Booking { CheckInDate = checkIn, CheckOutDate = checkOut });

            // Block covers the same dates — must reject.
            blockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RoomBlock>
            {
                new() { RoomId = 10,
                        StartDate = new DateTime(2026, 7, 11),
                        EndDate   = new DateTime(2026, 7, 12) }
            });

            var sut    = BuildBookingsController(bookingRepo, roomRepo, blockRepo, mapper,
                                                 roles: "Customer");
            var result = await sut.Create(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("blocked", conflict.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        // =====================================================================
        //  TEST 7 — BookingsController.Create → 201 when block dates don't overlap
        // =====================================================================

        [Fact]
        public async Task CreateBooking_Returns201_WhenBlockDatesDoNotOverlap()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var blockRepo   = new Mock<IRepository<RoomBlock>>();
            var mapper      = new Mock<IMapper>();

            var checkIn  = new DateTime(2026, 7, 10);
            var checkOut = new DateTime(2026, 7, 13);

            var dto = new BookingDTO
            {
                BookingType  = BookingType.NightStay,
                CheckInDate  = checkIn,
                CheckOutDate = checkOut,
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };

            roomRepo.Setup(r => r.GetByIdAsync(10))
                    .ReturnsAsync(new Room { Id = 10, RoomNumber = "R-10", PricePerNight = 100m });
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
            mapper.Setup(m => m.Map<Booking>(dto))
                  .Returns(new Booking { CheckInDate = checkIn, CheckOutDate = checkOut });
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            // Block is AFTER the requested window — no overlap.
            blockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RoomBlock>
            {
                new() { RoomId = 10,
                        StartDate = new DateTime(2026, 7, 20),
                        EndDate   = new DateTime(2026, 7, 25) }
            });

            var sut    = BuildBookingsController(bookingRepo, roomRepo, blockRepo, mapper,
                                                 roles: "Customer");
            var result = await sut.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        // =====================================================================
        //  TEST 8 — BookingsController.Create → 201 when block is on a different room
        // =====================================================================

        [Fact]
        public async Task CreateBooking_Returns201_WhenBlockIsOnDifferentRoom()
        {
            var bookingRepo = new Mock<IRepository<Booking>>();
            var roomRepo    = new Mock<IRepository<Room>>();
            var blockRepo   = new Mock<IRepository<RoomBlock>>();
            var mapper      = new Mock<IMapper>();

            var checkIn  = new DateTime(2026, 7, 10);
            var checkOut = new DateTime(2026, 7, 13);

            var dto = new BookingDTO
            {
                BookingType  = BookingType.NightStay,
                CheckInDate  = checkIn,
                CheckOutDate = checkOut,
                Rooms        = new List<BookingRoomDTO> { new() { RoomId = 10 } }
            };

            roomRepo.Setup(r => r.GetByIdAsync(10))
                    .ReturnsAsync(new Room { Id = 10, RoomNumber = "R-10", PricePerNight = 100m });
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
            mapper.Setup(m => m.Map<Booking>(dto))
                  .Returns(new Booking { CheckInDate = checkIn, CheckOutDate = checkOut });
            mapper.Setup(m => m.Map<BookingDTO>(It.IsAny<Booking>())).Returns(new BookingDTO());

            // Block is on Room 11, not Room 10 — must not block.
            blockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RoomBlock>
            {
                new() { RoomId = 11,   // different room
                        StartDate = checkIn,
                        EndDate   = checkOut }
            });

            var sut    = BuildBookingsController(bookingRepo, roomRepo, blockRepo, mapper,
                                                 roles: "Customer");
            var result = await sut.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }
    }
}
