import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { RoomDTO, RoomTypeDTO } from '../../model/api.models';

@Component({
  selector: 'app-rooms',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatTableModule, MatButtonModule, MatIconModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatCheckboxModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './rooms.component.html',
  styleUrl: './rooms.component.scss'
})
export class RoomsComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private snack = inject(MatSnackBar);

  rooms = signal<RoomDTO[]>([]);
  roomTypes = signal<RoomTypeDTO[]>([]);
  loading = signal(true);
  showForm = signal(false);
  editId = signal<number | null>(null);
  displayedColumns = ['roomNumber', 'roomTypeName', 'isAvailable', 'actions'];

  form = this.fb.group({
    roomNumber: ['', Validators.required],
    roomTypeId: [0, Validators.required],
    isAvailable: [true]
  });

  ngOnInit() {
    this.loadRooms();
    this.api.get<RoomTypeDTO[]>('roomtypes').subscribe({ next: d => this.roomTypes.set(d) });
  }

  loadRooms() {
    this.loading.set(true);
    this.api.get<RoomDTO[]>('rooms').subscribe({
      next: d => this.rooms.set(d),
      complete: () => this.loading.set(false)
    });
  }

  openForm(room?: RoomDTO) {
    if (room) {
      this.editId.set(room.id);
      this.form.patchValue({ roomNumber: room.roomNumber, roomTypeId: room.roomTypeId, isAvailable: room.isAvailable });
    } else {
      this.editId.set(null);
      this.form.reset({ isAvailable: true });
    }
    this.showForm.set(true);
  }

  save() {
    if (this.form.invalid) return;
    const payload = { ...this.form.value, id: this.editId() ?? 0 };
    const call = this.editId()
      ? this.api.put(`rooms/${this.editId()}`, payload)
      : this.api.post('rooms', payload);

    call.subscribe({
      next: () => { this.snack.open('Saved!', '', { duration: 2000 }); this.showForm.set(false); this.loadRooms(); },
      error: () => this.snack.open('Error saving room.', 'Dismiss', { duration: 3000 })
    });
  }

  delete(id: number) {
    if (!confirm('Delete this room?')) return;
    this.api.delete(`rooms/${id}`).subscribe({
      next: () => { this.snack.open('Deleted', '', { duration: 2000 }); this.loadRooms(); },
      error: () => this.snack.open('Error deleting room.', 'Dismiss', { duration: 3000 })
    });
  }
}
