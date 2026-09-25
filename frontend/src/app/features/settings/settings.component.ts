import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings.component.html'
})
export class SettingsComponent implements OnInit {
  settingsForm!: FormGroup;
  isLoading = true;
  isSaving = false;
  successMessage = '';
  logoPreview = '';

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);

  ngOnInit() {
    this.settingsForm = this.fb.group({
      companyName: ['', Validators.required],
      taxId: ['', [Validators.required, Validators.pattern('^[0-9]{13}$')]],
      address: ['', Validators.required],
      phone: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      logoUrl: ['']
    });

    this.loadSettings();
  }

  loadSettings() {
    this.isLoading = true;
    this.http.get<any>('http://localhost:5243/api/Settings/company').subscribe({
      next: (data) => {
        this.settingsForm.patchValue({
          companyName: data.companyName,
          taxId: data.taxId,
          address: data.address,
          phone: data.phone,
          email: data.email,
          logoUrl: data.logoUrl
        });
        this.logoPreview = data.logoUrl;
        this.isLoading = false;
      },
      error: () => {
        alert('Failed to load settings');
        this.isLoading = false;
      }
    });
  }

  onFileChange(event: any) {
    const file = event.target.files[0];
    if (file) {
      if (file.size > 2 * 1024 * 1024) {
        alert('File size must be less than 2MB');
        return;
      }
      const reader = new FileReader();
      reader.onload = () => {
        this.logoPreview = reader.result as string;
        this.settingsForm.patchValue({ logoUrl: this.logoPreview });
      };
      reader.readAsDataURL(file);
    }
  }

  onSubmit() {
    if (this.settingsForm.invalid) {
      this.settingsForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.successMessage = '';

    this.http.put('http://localhost:5243/api/Settings/company', this.settingsForm.value, { withCredentials: true }).subscribe({
      next: (res: any) => {
        this.isSaving = false;
        this.successMessage = 'Company settings saved successfully!';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        console.error(err);
        alert('Failed to save settings');
        this.isSaving = false;
      }
    });
  }
}
