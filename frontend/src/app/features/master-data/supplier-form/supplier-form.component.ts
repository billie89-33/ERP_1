import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-supplier-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-2xl mx-auto">
      <div class="mb-6 flex justify-between items-center">
        <div>
          <h1 class="text-2xl font-bold text-slate-800">{{ isEditMode ? 'แก้ไขข้อมูลซัพพลายเออร์' : 'เพิ่มซัพพลายเออร์ใหม่' }}</h1>
        </div>
        <a routerLink="/admin/suppliers" class="text-slate-500 hover:text-slate-700 font-medium">
          &larr; กลับหน้ารายการ
        </a>
      </div>

      <div class="bg-white shadow-md rounded-lg p-6 border border-slate-200">
        <form (ngSubmit)="saveSupplier()" #supplierForm="ngForm">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
            
            <div class="col-span-1 md:col-span-2">
              <label class="block text-sm font-medium text-slate-700 mb-1">ชื่อบริษัท / ร้านค้า <span class="text-red-500">*</span></label>
              <input type="text" name="companyName" [(ngModel)]="supplier.companyName" required
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">เลขประจำตัวผู้เสียภาษี <span class="text-red-500">*</span></label>
              <input type="text" name="taxId" [(ngModel)]="supplier.taxId" required
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">เบอร์โทรศัพท์</label>
              <input type="text" name="phone" [(ngModel)]="supplier.phone"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">ชื่อผู้ติดต่อ</label>
              <input type="text" name="contactName" [(ngModel)]="supplier.contactName"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>
            
            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">เครดิตเทอม (วัน)</label>
              <input type="number" name="creditTermDays" [(ngModel)]="supplier.creditTermDays"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div class="col-span-1 md:col-span-2">
              <label class="block text-sm font-medium text-slate-700 mb-1">ที่อยู่</label>
              <textarea name="address" [(ngModel)]="supplier.address" rows="3"
                        class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"></textarea>
            </div>
          </div>

          <div *ngIf="errorMessage" class="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded">
            {{ errorMessage }}
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-slate-200">
            <button type="button" routerLink="/admin/suppliers" class="px-4 py-2 border border-slate-300 rounded-md text-slate-700 hover:bg-slate-50 transition">
              ยกเลิก
            </button>
            <button type="submit" [disabled]="!supplierForm.form.valid || isSaving" class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:bg-blue-300">
              <span *ngIf="isSaving"><i class="fas fa-spinner fa-spin mr-2"></i> กำลังบันทึก...</span>
              <span *ngIf="!isSaving"><i class="fas fa-save mr-2"></i> บันทึกข้อมูล</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class SupplierFormComponent implements OnInit {
  supplier: any = { companyName: '', taxId: '', contactName: '', phone: '', address: '', creditTermDays: 30 };
  isEditMode = false;
  isSaving = false;
  errorMessage = '';
  supplierId: string | null = null;

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  ngOnInit() {
    this.supplierId = this.route.snapshot.paramMap.get('id');
    if (this.supplierId) {
      this.isEditMode = true;
      this.loadSupplier();
    }
  }

  loadSupplier() {
    this.http.get<any>(`/api/Suppliers/${this.supplierId}`).subscribe({
      next: (data) => this.supplier = data,
      error: () => this.errorMessage = 'ไม่สามารถโหลดข้อมูลซัพพลายเออร์ได้'
    });
  }

  saveSupplier() {
    this.isSaving = true;
    this.errorMessage = '';

    const req = this.isEditMode 
      ? this.http.put(`/api/Suppliers/${this.supplierId}`, this.supplier)
      : this.http.post('/api/Suppliers', this.supplier);

    req.subscribe({
      next: () => {
        this.isSaving = false;
        this.router.navigate(['/admin/suppliers']);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกข้อมูล';
      }
    });
  }
}
