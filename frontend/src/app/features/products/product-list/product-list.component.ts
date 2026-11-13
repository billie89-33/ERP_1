import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { AiService } from '../../../core/services/ai.service';
import { ProductDto } from '../../../core/models/product.model';
import { AiStatusDto } from '../../../core/models/ai.model';
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
  aiStatus: AiStatusDto | null = null;
  isSyncingEmbeddings = false;
  syncResultMessage = '';
  
  // Pagination State
  page = 1;
  limit = 10;
  total = 0;
  search = '';
  searchTimeout: ReturnType<typeof setTimeout> | null = null;
  
  private productService = inject(ProductService);
  private aiService = inject(AiService);

  ngOnInit() {
    this.loadProducts();
    this.loadAiStatus();
  }

  loadAiStatus() {
    this.aiService.getStatus().subscribe({
      next: (status) => {
        this.aiStatus = status;
      },
      error: () => {
        this.aiStatus = null;
      }
    });
  }

  syncAllEmbeddings(forceRebuildAll = false) {
    if (this.isSyncingEmbeddings) return;
    this.isSyncingEmbeddings = true;
    this.syncResultMessage = '';

    this.aiService.syncEmbeddings({ forceRebuildAll }).subscribe({
      next: (res) => {
        this.isSyncingEmbeddings = false;
        this.syncResultMessage = res.message;
        this.loadAiStatus();
      },
      error: (err: { error?: { message?: string } }) => {
        this.isSyncingEmbeddings = false;
        this.syncResultMessage = err?.error?.message || 'ไม่สามารถซิงค์ Vector Embeddings ได้ กรุณาตรวจสอบ GEMINI_API_KEY';
      }
    });
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

  toggleFeatured(product: ProductDto) {
    const originalState = product.isFeatured;
    product.isFeatured = !product.isFeatured; // Optimistic update
    
    this.productService.toggleFeaturedProduct(product.id).subscribe({
      next: (res) => {
        product.isFeatured = res.isFeatured;
      },
      error: (err) => {
        console.error("Failed to toggle featured status", err);
        product.isFeatured = originalState; // Revert on failure
        alert("Failed to update featured status.");
      }
    });
  }

  deleteProduct(id: string) {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.deleteProduct(id).subscribe(() => {
        this.loadProducts();
        this.loadAiStatus();
      });
    }
  }
}
