import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ApiMessageResponse, CompanySettingsDto } from '../../core/models/erp.model';

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
  heroPreview = '';

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);

  ngOnInit() {
    this.settingsForm = this.fb.group({
      companyName: ['', Validators.required],
      taxId: ['', [Validators.required, Validators.pattern('^[0-9]{13}$')]],
      address: ['', Validators.required],
      phone: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      logoUrl: [''],
      heroBadge: ['New Generation Components'],
      heroTitle: ['Jamine CPU คอมพิวเตอร์เซตสเปกแรง ตอบโจทย์ทุกการใช้งาน'],
      heroSubtitle: ['คัดสรรอุปกรณ์ไอทีคุณภาพ ประกันศูนย์แท้ 100% ประกอบและเทสระบบก่อนจัดส่ง พร้อมผู้ช่วย AI แนะนำสเปคตามงบประมาณ'],
      heroImageUrl: [''],
      heroCtaText: ['เลือกซื้อสินค้าเลย']
    });

    this.loadSettings();
  }

  loadSettings() {
    this.isLoading = true;
    this.http.get<CompanySettingsDto>('/api/Settings/company').subscribe({
      next: (data) => {
        this.settingsForm.patchValue({
          companyName: data.companyName,
          taxId: data.taxId,
          address: data.address,
          phone: data.phone,
          email: data.email,
          logoUrl: data.logoUrl,
          heroBadge: data.heroBadge || 'New Generation Components',
          heroTitle: data.heroTitle || 'Jamine CPU คอมพิวเตอร์เซตสเปกแรง ตอบโจทย์ทุกการใช้งาน',
          heroSubtitle: data.heroSubtitle || 'คัดสรรอุปกรณ์ไอทีคุณภาพ ประกันศูนย์แท้ 100% ประกอบและเทสระบบก่อนจัดส่ง พร้อมผู้ช่วย AI แนะนำสเปคตามงบประมาณ',
          heroImageUrl: data.heroImageUrl || '',
          heroCtaText: data.heroCtaText || 'เลือกซื้อสินค้าเลย'
        });
        this.logoPreview = data.logoUrl || '';
        this.heroPreview = data.heroImageUrl || '';
        this.isLoading = false;
      },
      error: () => {
        alert('Failed to load settings');
        this.isLoading = false;
      }
    });
  }

  onFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
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

  onHeroFileChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      if (file.size > 2 * 1024 * 1024) {
        alert('Hero image size must be less than 2MB');
        return;
      }
      const reader = new FileReader();
      reader.onload = () => {
        this.heroPreview = reader.result as string;
        this.settingsForm.patchValue({ heroImageUrl: this.heroPreview });
      };
      reader.readAsDataURL(file);
    }
  }

  onHeroUrlInput(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.heroPreview = val;
  }

  resetHeroImage() {
    this.heroPreview = '';
    this.settingsForm.patchValue({ heroImageUrl: '' });
  }

  onSubmit() {
    if (this.settingsForm.invalid) {
      this.settingsForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.successMessage = '';

    this.http.put<ApiMessageResponse>('/api/Settings/company', this.settingsForm.value, { withCredentials: true }).subscribe({
      next: () => {
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
