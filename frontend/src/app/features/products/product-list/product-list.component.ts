import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { ProductDto } from '../../../core/models/product.model';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PaginationComponent],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  products: ProductDto[] = [];
  
  // Pagination State
  page = 1;
  limit = 10;
  total = 0;
  search = '';
  searchTimeout: any;
  
  private productService = inject(ProductService);

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.productService.getProducts({ page: this.page, limit: this.limit, searchQuery: this.search }).subscribe(res => {
      this.products = res.data;
      this.total = res.total;
    });
  }

  onSearchChange() {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.page = 1;
      this.loadProducts();
    }, 500);
  }

  onPageChange(newPage: number) {
    this.page = newPage;
    this.loadProducts();
  }

  deleteProduct(id: string) {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.deleteProduct(id).subscribe(() => {
        this.loadProducts();
      });
    }
  }
}
