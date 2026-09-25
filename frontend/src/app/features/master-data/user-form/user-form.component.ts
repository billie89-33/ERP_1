import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="max-w-2xl mx-auto">
      <div class="mb-6 flex justify-between items-center">
        <div>
          <h1 class="text-2xl font-bold text-slate-800">{{ isEditMode ? 'แก้ไขบัญชีผู้ใช้' : 'เพิ่มบัญชีใหม่' }}</h1>
        </div>
        <a routerLink="/admin/users" class="text-slate-500 hover:text-slate-700 font-medium">
          &larr; กลับหน้ารายการ
        </a>
      </div>

    <div class="bg-white shadow-md rounded-lg p-6 border border-slate-200">
        <form (ngSubmit)="saveUser()" #userForm="ngForm">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
            
            <div class="col-span-1">
              <label class="block text-sm font-medium text-slate-700 mb-1">ชื่อผู้ใช้งาน (Username) <span class="text-red-500">*</span></label>
              <input type="text" name="username" [(ngModel)]="user.username" required
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div class="col-span-1">
              <label class="block text-sm font-medium text-slate-700 mb-1">สิทธิ์ (Role) <span class="text-red-500">*</span></label>
              <select name="role" [(ngModel)]="user.role" required
                      class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                <option value="Admin">Admin (ผู้ดูแลระบบ)</option>
                <option value="Sales">Sales (ฝ่ายขาย)</option>
                <option value="Purchasing">Purchasing (ฝ่ายจัดซื้อ)</option>
                <option value="Warehouse">Warehouse (ฝ่ายคลังสินค้า)</option>
                <option value="Customer">Customer (ลูกค้าหน้าเว็บ)</option>
              </select>
            </div>

            <div class="col-span-1 md:col-span-2">
              <label class="block text-sm font-medium text-slate-700 mb-1">รหัสผ่าน <span class="text-red-500" *ngIf="!isEditMode">*</span></label>
              <input type="password" name="password" [(ngModel)]="user.password" [required]="!isEditMode"
                     [placeholder]="isEditMode ? 'เว้นว่างไว้ถ้าไม่ต้องการเปลี่ยน' : ''"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div class="col-span-1">
              <label class="block text-sm font-medium text-slate-700 mb-1">ชื่อจริง (First Name)</label>
              <input type="text" name="firstName" [(ngModel)]="user.firstName"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div class="col-span-1">
              <label class="block text-sm font-medium text-slate-700 mb-1">นามสกุล (Last Name)</label>
              <input type="text" name="lastName" [(ngModel)]="user.lastName"
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>

            <div class="col-span-1 md:col-span-2">
              <label class="block text-sm font-medium text-slate-700 mb-1">อีเมล (Email) <span class="text-red-500">*</span></label>
              <input type="email" name="email" [(ngModel)]="user.email" required
                     class="w-full px-3 py-2 border border-slate-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            </div>
            
          </div>

          <div *ngIf="errorMessage" class="mb-4 bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded">
            {{ errorMessage }}
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-slate-200">
            <button type="button" routerLink="/admin/users" class="px-4 py-2 border border-slate-300 rounded-md text-slate-700 hover:bg-slate-50 transition">
              ยกเลิก
            </button>
            <button type="submit" [disabled]="!userForm.form.valid || isSaving" class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:bg-blue-300">
              <span *ngIf="isSaving"><i class="fas fa-spinner fa-spin mr-2"></i> กำลังบันทึก...</span>
              <span *ngIf="!isSaving"><i class="fas fa-save mr-2"></i> บันทึกข้อมูล</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class UserFormComponent implements OnInit {
  user: any = { username: '', password: '', role: 'Sales', firstName: '', lastName: '', email: '' };
  isEditMode = false;
  isSaving = false;
  errorMessage = '';
  userId: string | null = null;

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  ngOnInit() {
    this.userId = this.route.snapshot.paramMap.get('id');
    if (this.userId) {
      this.isEditMode = true;
      this.loadUser();
    }
  }

  loadUser() {
    this.http.get<any>(`/api/Users/${this.userId}`).subscribe({
      next: (data) => {
        this.user.username = data.username;
        this.user.role = data.role;
        this.user.firstName = data.firstName;
        this.user.lastName = data.lastName;
        this.user.email = data.email;
        // Do not load password
      },
      error: () => this.errorMessage = 'ไม่สามารถโหลดข้อมูลผู้ใช้ได้'
    });
  }

  saveUser() {
    this.isSaving = true;
    this.errorMessage = '';

    let payload: any;
    if (this.isEditMode) {
      payload = { 
        username: this.user.username, 
        role: this.user.role,
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        email: this.user.email
      };
      if (this.user.password) {
        payload.newPassword = this.user.password;
      }
    } else {
      payload = this.user;
    }

    const req = this.isEditMode 
      ? this.http.put(`/api/Users/${this.userId}`, payload)
      : this.http.post('/api/Users', payload);

    req.subscribe({
      next: () => {
        this.isSaving = false;
        this.router.navigate(['/admin/users']);
      },
      error: (err) => {
        this.isSaving = false;
        this.errorMessage = err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกข้อมูล';
      }
    });
  }
}
