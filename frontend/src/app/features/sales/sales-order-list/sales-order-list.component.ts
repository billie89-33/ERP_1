import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-sales-order-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './sales-order-list.component.html',
  styleUrl: './sales-order-list.component.scss'
})
export class SalesOrderListComponent implements OnInit {
  private http = inject(HttpClient);
  
  salesOrders: any[] = [];
  isLoading = true;

  ngOnInit() {
    this.loadSOs();
  }

  loadSOs() {
    this.isLoading = true;
    this.http.get<any[]>('http://localhost:5243/api/SalesOrders').subscribe({
      next: (data) => {
        this.salesOrders = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }
}
