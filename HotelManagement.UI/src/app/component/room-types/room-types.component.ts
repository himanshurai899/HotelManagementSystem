import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { RoomTypeDTO } from '../../model/api.models';

@Component({
  selector: 'app-room-types',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatTableModule, MatButtonModule, MatIconModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './room-types.component.html',
  styleUrl: './room-types.component.scss'
})
export class RoomTypesComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private snack = inject(MatSnackBar);

  items = signal<RoomTypeDTO[]>([]);
  loading = signal(true);
  showForm = signal(false);
  editId = signal<number | null>(null);
  displayedColumns = ['name', 'basePrice', 'capacity', 'actions'];

  form = this.fb.group({
    name: ['', Validators.required],
    basePrice: [0, [Validators.required, Validators.min(0)]],
    capacity: [1, [Validators.required, Validators.min(1)]]
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.get<RoomTypeDTO[]>('roomtypes').subscribe({ next: d => this.items.set(d), complete: () => this.loading.set(false) });
  }

  openForm(item?: RoomTypeDTO) {
    this.editId.set(item?.id ?? null);
    this.form.patchValue(item ?? { name: '', basePrice: 0, capacity: 1 });
    this.showForm.set(true);
  }

  save() {
    if (this.form.invalid) return;
    const payload = { ...this.form.value, id: this.editId() ?? 0 };
    const call = this.editId() ? this.api.put(`roomtypes/${this.editId()}`, payload) : this.api.post('roomtypes', payload);
    call.subscribe({
      next: () => { this.snack.open('Saved!', '', { duration: 2000 }); this.showForm.set(false); this.load(); },
      error: () => this.snack.open('Error saving.', 'Dismiss', { duration: 3000 })
    });
  }

  delete(id: number) {
    if (!confirm('Delete this room type?')) return;
    this.api.delete(`roomtypes/${id}`).subscribe({ next: () => { this.snack.open('Deleted', '', { duration: 2000 }); this.load(); } });
  }
}
