import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PaginationComponent],
  template: `
    <div class="mb-6 flex justify-between items-center">
      <div>
        <h1 class="text-2xl font-bold text-slate-800">จัดการพนักงาน (Users)</h1>
        <p class="text-sm text-slate-500 mt-1">ตั้งค่าบัญชีผู้ใช้งาน และกำหนดสิทธิ์ Role</p>
      </div>
      <a routerLink="/admin/users/create" class="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded shadow-sm transition flex items-center">
        <i class="fas fa-plus mr-2"></i> เพิ่มผู้ใช้งาน
      </a>
    </div>

    <div class="bg-white shadow-md rounded-lg overflow-hidden border border-slate-200">
      <div class="p-4 border-b border-slate-200 bg-slate-50 flex justify-between items-center">
        <div class="relative w-64">
          <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <i class="fas fa-search text-slate-400"></i>
          </div>
          <input type="text" [(ngModel)]="searchQuery" (ngModelChange)="onSearchChange()" placeholder="Search username, email..." class="pl-10 focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-slate-300 rounded-md">
        </div>
      </div>

      <div *ngIf="isLoading" class="p-12 text-center text-slate-500">
        <i class="fas fa-spinner fa-spin text-3xl mb-3 text-blue-500"></i>
        <p>กำลังโหลดข้อมูลผู้ใช้งาน...</p>
      </div>
      
      <div *ngIf="!isLoading" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-slate-200 text-sm">
          <thead class="bg-slate-100">
            <tr>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">ชื่อผู้ใช้งาน (Username)</th>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">ชื่อ-นามสกุล</th>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">อีเมล</th>
              <th class="px-6 py-3 text-left font-bold text-slate-600 uppercase">สิทธิ์ (Role)</th>
              <th class="px-6 py-3 text-right font-bold text-slate-600 uppercase">การจัดการ</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-slate-200">
            <tr *ngFor="let user of users" class="hover:bg-slate-50 transition-colors">
              <td class="px-6 py-4 font-bold text-slate-800">{{ user.username }}</td>
              <td class="px-6 py-4 text-slate-600">{{ user.firstName }} {{ user.lastName }}</td>
              <td class="px-6 py-4 text-slate-600">{{ user.email }}</td>
              <td class="px-6 py-4">
                <span class="px-2 py-1 rounded text-xs font-bold"
                      [ngClass]="{
                        'bg-blue-100 text-blue-800': user.role === 'Admin',
                        'bg-green-100 text-green-800': user.role === 'Sales',
                        'bg-purple-100 text-purple-800': user.role === 'Purchasing',
                        'bg-yellow-100 text-yellow-800': user.role === 'Warehouse'
                      }">
                  {{ user.role }}
                </span>
              </td>
              <td class="px-6 py-4 text-right">
                <a [routerLink]="['/admin/users/edit', user.id]" class="text-blue-600 hover:text-blue-900 font-medium">แก้ไข</a>
              </td>
            </tr>
            <tr *ngIf="users.length === 0">
              <td colspan="3" class="px-6 py-12 text-center text-slate-500">
                <i class="fas fa-user-shield text-4xl mb-3 text-slate-300"></i>
                <p>ยังไม่มีข้อมูลผู้ใช้งาน</p>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <app-pagination 
        *ngIf="!isLoading && totalCount > 0"
        [totalCount]="totalCount" 
        [pageSize]="pageSize" 
        [currentPage]="page" 
        (pageChange)="onPageChange($event)">
      </app-pagination>
    </div>
  `
})
export class UserListComponent implements OnInit {
  users: any[] = [];
  isLoading = true;

  totalCount = 0;
  page = 1;
  pageSize = 10;
  searchQuery = '';
  searchTimeout: any;

  private http = inject(HttpClient);

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.isLoading = true;
    this.http.get<any>(`/api/Users?page=${this.page}&pageSize=${this.pageSize}&search=${this.searchQuery}`).subscribe({
      next: (data) => {
        this.users = data.items;
        this.totalCount = data.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load users', err);
        this.isLoading = false;
      }
    });
  }

  onSearchChange() {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.page = 1;
      this.loadUsers();
    }, 500);
  }

  onPageChange(newPage: number) {
    this.page = newPage;
    this.loadUsers();
  }
}
