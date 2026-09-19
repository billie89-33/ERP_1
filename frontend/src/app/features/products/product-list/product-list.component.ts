import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';

import { ProductDto } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  products: ProductDto[] = [];
  page = 1;
  limit = 10;
  total = 0;
  totalPages = 0;
  search = '';
  
  private productService = inject(ProductService);

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.productService.getProducts({ page: this.page, limit: this.limit, searchQuery: this.search }).subscribe(res => {
      this.products = res.data;
      this.total = res.total;
      this.totalPages = res.totalPages;
    });
  }

  onSearch(event: Event) {
    const target = event.target as HTMLInputElement;
    this.search = target.value;
    this.page = 1;
    this.loadProducts();
  }

  nextPage() {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadProducts();
    }
  }

  prevPage() {
    if (this.page > 1) {
      this.page--;
      this.loadProducts();
    }
  }

  deleteProduct(id: string) {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.deleteProduct(id).subscribe(() => {
        this.loadProducts();
      });
    }
  }
}
