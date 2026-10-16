import { AbstractControl, ValidationErrors } from '@angular/forms';

export function validBirthDate(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) return null; // Required is handled separately.
  if (typeof value !== 'string' || !/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(value)) {
    return { birthDate: true };
  }
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(0);
  date.setHours(0, 0, 0, 0);
  date.setFullYear(year, month - 1, day);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return year < 1 || date.getFullYear() !== year || date.getMonth() !== month - 1 ||
    date.getDate() !== day || date > today ? { birthDate: true } : null;
}
