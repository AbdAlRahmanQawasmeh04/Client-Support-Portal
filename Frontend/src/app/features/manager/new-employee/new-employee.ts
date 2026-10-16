import { validBirthDate } from '../../../core/birth-date-validator';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { API_URL } from '../../../core/auth.service';

@Component({selector:'app-new-employee', imports:[ReactiveFormsModule, RouterLink], templateUrl:'./new-employee.html', styleUrl:'./new-employee.css'})
export class NewEmployee {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly busy = signal(false);
  readonly done = signal(false);
  readonly createdName = signal(' ');
  readonly error = signal('');
  readonly imageError = signal('');
  readonly preview = signal('');
  readonly showPassword = signal(false);
  readonly attempted = signal(false);
  private image: File | null = null;
  readonly today = (() => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; })();
  readonly form = this.fb.nonNullable.group({
    fullName:['', Validators.required], username:['', Validators.required],
    email:['', [Validators.required, Validators.email]], mobileNumber:['', [Validators.required, Validators.pattern(/^\+?[0-9() .-]{6,25}$/)]],
    dateOfBirth:['', [Validators.required, validBirthDate]], address:['', Validators.required],
    password:['', [Validators.required, Validators.minLength(8)]], confirmPassword:['', Validators.required],
  });
  constructor() { this.destroyRef.onDestroy(() => this.clearPreview()); }
  invalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.attempted());
  }
  private clearPreview(): void { if (this.preview()) URL.revokeObjectURL(this.preview()); this.preview.set(''); }
  selectImage(event: Event): void {
    if (this.busy() || this.done()) return;
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    this.clearPreview(); this.image = null; this.imageError.set('');
    if (!file) return;
    if (!/\.(png|jpe?g)$/i.test(file.name) || !['image/png','image/jpeg'].includes(file.type) || file.size === 0 || file.size > 2*1024*1024) {
      this.imageError.set('Choose a JPG or PNG image, up to 2 MB.'); input.value = ''; return;
    }
    this.image = file; this.preview.set(URL.createObjectURL(file));
  }
  removeImage(input: HTMLInputElement): void {
    if (this.busy() || this.done()) return;
    this.image = null; this.clearPreview(); this.imageError.set(''); input.value = '';
  }
  submit(): void {
    if (this.busy() || this.done()) return;
    this.attempted.set(true); this.error.set(''); this.form.markAllAsTouched();
    const value = this.form.getRawValue();
    if (this.form.invalid || this.imageError()) { this.error.set('Please check the highlighted fields and image.'); return; }
    if (['fullName','username','address'].some(key => !value[key as keyof typeof value].trim())) { this.error.set('Name, username and address cannot be blank.'); return; }
    if (!/^\d{4}-\d{2}-\d{2}$/.test(value.dateOfBirth) || !Number.isFinite(Date.parse(value.dateOfBirth)) || value.dateOfBirth > this.today) { this.error.set('Choose a valid date of birth that is not in the future.'); return; }
    if (value.password !== value.confirmPassword) { this.error.set('The passwords do not match.'); return; }
    const data = new FormData();
    for (const key of ['fullName','username','email','mobileNumber','dateOfBirth','address','password'] as const) {
      data.append(key[0].toUpperCase()+key.slice(1), key === 'password' ? value[key] : value[key].trim());
    }
    if (this.image) data.append('Image', this.image);
    this.busy.set(true);
    this.http.post(API_URL + '/employees', data).pipe(takeUntilDestroyed(this.destroyRef), finalize(()=>this.busy.set(false))).subscribe({
      next:()=>{ this.createdName.set(value.fullName.trim()); this.done.set(true); this.form.controls.password.reset(); this.form.controls.confirmPassword.reset(); this.image = null; this.clearPreview(); },
      error:(response:HttpErrorResponse)=>{
        let message = response.status === 0 ? 'Cannot reach the server. Please try again.' : response.status === 409 ? 'This username or email is already registered.' : 'Could not create the employee account. Please try again.';
        if (response.status === 400) {
          message = 'Please check your details and image.';
          if (typeof response.error === 'string') message = response.error;
          else if (response.error?.errors) {
            const errors = Object.values(response.error.errors).flat().filter((v):v is string => typeof v === 'string');
            if (errors.length) message = errors.join(' ');
          }
        }
        this.error.set(message);
      }
    });
  }
}

