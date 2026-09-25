import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-2xl mx-auto">
      <div class="mb-6 flex justify-between items-center">
        <div>
          <h1 class="text-2xl font-bold text-slate-800">{{ isEditMode ? 'แก้ไขข้อมูลลูกค้า' : 'เพิ่มลูกค้าใหม่' }}</h1>
        </div>
        <a routerLink="/admin/customers" class="text-slate-500 hover:text-slate-700 font-medium">
          &larr; กลับหน้ารายการ
        </a>
      </div>

      <div class="bg-white shadow-md rounded-lg p-6 border border-slate-200">
        <form (ngSubmit)="saveCustomer()" #customerForm="ngForm">
          
          <div class="mb-6">
            <label class="block text-sm font-medium text-slate-700 mb-2">ประเภทลูกค้า <span class="text-red-500">*</span></label>
            <div class="flex gap-4">
              <label class="inline-flex items-center">
                <input type="radio" name="customerType" [(ngModel)]="customer.customerType" value="B2B" class="form-radio text-blue-600" (change)="onTypeChange()">
                <span class="ml-2">🏢 นิติบุคคล (B2B)</span>
              </label>
              <label class="inline-flex items-center">
                <input type="radio" name="customerType" [(ngModel)]="customer.customerType" value="B2C" class="form-radio text-blue-600" (change)="onTypeChange()">
                <span class="ml-2">👤 บุคคลธรรมดา (B2C)</span>
              </label>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
            
            <div class="col-span-1 md:col-span-2">
              <label class="block text-sm font-medium text-slate-700 mb-1">
                {{ customer.customerType === 'B2B' ? 'ชื่อบริษัท' : 'ชื่อ-นามสกุล' }} <span class="text-red-500">*</span>
              </label>
              <input type="text" name="companyName" [(ngModel)]="customer.companyName" required
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">เลขประจำตัวผู้เสียภาษี <span class="text-red-500" *ngIf="customer.customerType === 'B2B'">*</span></label>
              <input type="text" name="taxId" [(ngModel)]="customer.taxId" [required]="customer.customerType === 'B2B'" [minlength]="customer.taxId ? 13 : 0" [maxlength]="13"
                     [placeholder]="customer.customerType === 'B2B' ? 'เลข 13 หลัก' : 'เว้นว่างได้'"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
              <div *ngIf="customer.taxId && customer.taxId.length !== 13 && customerForm.controls['taxId']?.dirty" class="text-red-500 text-xs mt-1">
                เลขประจำตัวผู้เสียภาษีต้องมี 13 หลัก
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">เบอร์โทรศัพท์</label>
              <input type="text" name="phone" [(ngModel)]="customer.phone"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div class="col-span-1 md:col-span-2">
              <label class="block text-sm font-medium text-slate-700 mb-1">อีเมล</label>
              <input type="email" name="email" [(ngModel)]="customer.email"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <ng-container *ngIf="customer.customerType === 'B2B'">
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">เครดิตเทอม (วัน) <span class="text-red-500">*</span></label>
                <input type="number" name="creditTermDays" [(ngModel)]="customer.creditTermDays" required
                       class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
              </div>

              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">วงเงินเครดิต (บาท) <span class="text-red-500">*</span></label>
                <input type="number" name="creditLimit" [(ngModel)]="customer.creditLimit" required
                       class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
              </div>
            </ng-container>

            <div class="col-span-1 md:col-span-2">
              <label class="block text-sm font-medium text-slate-700 mb-1">ที่อยู่</label>
              <textarea name="address" [(ngModel)]="customer.address" rows="3"
                        class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"></textarea>
            </div>
          </div>

          <div *ngIf="errorMessage" class="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded">
            {{ errorMessage }}
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-slate-200">
            <button type="button" routerLink="/admin/customers" class="px-4 py-2 border border-slate-300 rounded-md text-slate-700 hover:bg-slate-50 transition">
              ยกเลิก
            </button>
            <button type="submit" [disabled]="!customerForm.form.valid || isSaving || (customer.taxId && customer.taxId.length !== 13)" class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:bg-blue-300 disabled:cursor-not-allowed">
              <span *ngIf="isSaving"><i class="fas fa-spinner fa-spin mr-2"></i> กำลังบันทึก...</span>
              <span *ngIf="!isSaving"><i class="fas fa-save mr-2"></i> บันทึกข้อมูล</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class CustomerFormComponent implements OnInit {
  customer: any = { customerType: 'B2B', companyName: '', taxId: '', address: '', phone: '', email: '', creditTermDays: 30, creditLimit: 0 };
  isEditMode = false;
  isSaving = false;
  errorMessage = '';
  customerId: string | null = null;

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  ngOnInit() {
    this.customerId = this.route.snapshot.paramMap.get('id');
    if (this.customerId) {
      this.isEditMode = true;
      this.loadCustomer();
    }
  }

  loadCustomer() {
    this.http.get<any>(`/api/Customers/${this.customerId}`).subscribe({
      next: (data) => this.customer = data,
      error: () => this.errorMessage = 'ไม่สามารถโหลดข้อมูลลูกค้าได้'
    });
  }

  onTypeChange() {
    if (this.customer.customerType === 'B2C') {
      this.customer.creditTermDays = 0;
      this.customer.creditLimit = 0;
    } else {
      if (this.customer.creditTermDays === 0) this.customer.creditTermDays = 30;
    }
  }

  saveCustomer() {
    this.isSaving = true;
    this.errorMessage = '';

    const req = this.isEditMode 
      ? this.http.put(`/api/Customers/${this.customerId}`, this.customer)
      : this.http.post('/api/Customers', this.customer);

    req.subscribe({
      next: () => {
        this.isSaving = false;
        this.router.navigate(['/admin/customers']);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกข้อมูล';
      }
    });
  }
}
