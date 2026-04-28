import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../services/api.service';
import {
  RoomBlockDTO,
  RoomBlockType,
  ROOM_BLOCK_TYPE_LABELS,
  RoomDTO
} from '../../model/api.models';

@Component({
  selector: 'app-room-blocks',
  standalone: true,
  imports: [
    ReactiveFormsModule, DatePipe,
    MatTableModule, MatButtonModule, MatIconModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatDatepickerModule, MatNativeDateModule,
    MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './room-blocks.component.html',
  styleUrl: './room-blocks.component.scss'
})
export class RoomBlocksComponent implements OnInit {
  private api   = inject(ApiService);
  private fb    = inject(FormBuilder);
  private snack = inject(MatSnackBar);

  blocks   = signal<RoomBlockDTO[]>([]);
  rooms    = signal<RoomDTO[]>([]);
  loading  = signal(true);
  showForm = signal(false);
  editId   = signal<number | null>(null);

  readonly RoomBlockType       = RoomBlockType;
  readonly blockTypeLabels     = ROOM_BLOCK_TYPE_LABELS;
  readonly blockTypeOptions    = [
    { value: RoomBlockType.Maintenance,    label: 'Maintenance'    },
    { value: RoomBlockType.Renovation,     label: 'Renovation'     },
    { value: RoomBlockType.VIPHold,        label: 'VIP Hold'       },
    { value: RoomBlockType.Administrative, label: 'Administrative' }
  ];

  displayedColumns = ['roomNumber', 'startDate', 'endDate', 'blockType', 'reason', 'actions'];

  form = this.fb.group({
    roomId:    [0,  Validators.required],
    startDate: [null as Date | null, Validators.required],
    endDate:   [null as Date | null, Validators.required],
    reason:    ['', Validators.required],
    blockType: [RoomBlockType.Maintenance as RoomBlockType, Validators.required]
  });

  today = new Date();

  ngOnInit() {
    this.loadBlocks();
    this.api.get<RoomDTO[]>('rooms').subscribe({ next: d => this.rooms.set(d) });
  }

  loadBlocks() {
    this.loading.set(true);
    this.api.get<RoomBlockDTO[]>('roomblocks').subscribe({
      next:  d => { this.blocks.set(d); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openAdd() {
    this.editId.set(null);
    this.form.reset({
      roomId:    0,
      startDate: null,
      endDate:   null,
      reason:    '',
      blockType: RoomBlockType.Maintenance
    });
    this.showForm.set(true);
  }

  openEdit(block: RoomBlockDTO) {
    this.editId.set(block.id);
    this.form.patchValue({
      roomId:    block.roomId,
      startDate: new Date(block.startDate),
      endDate:   new Date(block.endDate),
      reason:    block.reason,
      blockType: block.blockType
    });
    this.showForm.set(true);
  }

  save() {
    if (this.form.invalid) return;
    const v = this.form.value;
    const payload = {
      roomId:    v.roomId,
      startDate: (v.startDate as Date).toISOString(),
      endDate:   (v.endDate   as Date).toISOString(),
      reason:    v.reason,
      blockType: v.blockType
    };

    const id = this.editId();
    const req = id
      ? this.api.put<RoomBlockDTO>(`roomblocks/${id}`, payload)
      : this.api.post<RoomBlockDTO>('roomblocks', payload);

    req.subscribe({
      next: () => {
        this.snack.open(id ? 'Block updated.' : 'Block created.', 'OK', { duration: 3000 });
        this.showForm.set(false);
        this.loadBlocks();
      },
      error: (err) => {
        const msg = err?.error?.message ?? 'Save failed.';
        this.snack.open(msg, 'OK', { duration: 4000 });
      }
    });
  }

  delete(id: number) {
    if (!confirm('Delete this room block?')) return;
    this.api.delete(`roomblocks/${id}`).subscribe({
      next: () => {
        this.snack.open('Block deleted.', 'OK', { duration: 3000 });
        this.loadBlocks();
      },
      error: () => this.snack.open('Delete failed.', 'OK', { duration: 3000 })
    });
  }

  cancel() {
    this.showForm.set(false);
  }

  blockTypeLabel(type: number): string {
    return this.blockTypeLabels[type] ?? 'Unknown';
  }
}
