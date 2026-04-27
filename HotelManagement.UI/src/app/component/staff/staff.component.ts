import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { StaffDTO } from '../../model/api.models';

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [
    ReactiveFormsModule, DatePipe, MatTableModule, MatButtonModule, MatIconModule,
    MatCardModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './staff.component.html',
  styleUrl: './staff.component.scss'
})
export class StaffComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private snack = inject(MatSnackBar);

  items = signal<StaffDTO[]>([]);
  loading = signal(true);
  showForm = signal(false);
  editId = signal<number | null>(null);
  displayedColumns = ['name', 'email', 'position', 'hireDate', 'actions'];

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    position: ['', Validators.required],
    hireDate: ['', Validators.required]
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.get<StaffDTO[]>('staff').subscribe({ next: d => this.items.set(d), complete: () => this.loading.set(false) });
  }

  openForm(item?: StaffDTO) {
    this.editId.set(item?.id ?? null);
    if (item) {
      this.form.patchValue({ ...item, hireDate: item.hireDate.split('T')[0] });
    } else {
      this.form.reset();
    }
    this.showForm.set(true);
  }

  save() {
    if (this.form.invalid) return;
    const payload = { ...this.form.value, id: this.editId() ?? 0 };
    const call = this.editId() ? this.api.put(`staff/${this.editId()}`, payload) : this.api.post('staff', payload);
    call.subscribe({
      next: () => { this.snack.open('Saved!', '', { duration: 2000 }); this.showForm.set(false); this.load(); },
      error: () => this.snack.open('Error saving.', 'Dismiss', { duration: 3000 })
    });
  }

  delete(id: number) {
    if (!confirm('Remove this staff member?')) return;
    this.api.delete(`staff/${id}`).subscribe({ next: () => { this.snack.open('Deleted', '', { duration: 2000 }); this.load(); } });
  }
}
