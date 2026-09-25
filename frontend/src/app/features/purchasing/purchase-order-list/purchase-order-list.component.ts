import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-purchase-order-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PaginationComponent],
  templateUrl: './purchase-order-list.component.html',
  styleUrl: './purchase-order-list.component.scss'
})
export class PurchaseOrderListComponent implements OnInit {
  private http = inject(HttpClient);

  purchaseOrders: any[] = [];
  isLoading = true;

  totalCount = 0;
  page = 1;
  pageSize = 10;
  searchQuery = '';
  searchTimeout: any;

  ngOnInit() {
    this.loadPOs();
  }

  loadPOs() {
    this.isLoading = true;
    this.http.get<any>(`/api/PurchaseOrders?page=${this.page}&pageSize=${this.pageSize}&search=${this.searchQuery}`).subscribe({
      next: (data) => {
        this.purchaseOrders = data.items;
        this.totalCount = data.totalCount;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  onSearchChange() {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.page = 1;
      this.loadPOs();
    }, 500);
  }

  onPageChange(newPage: number) {
    this.page = newPage;
    this.loadPOs();
  }
}
