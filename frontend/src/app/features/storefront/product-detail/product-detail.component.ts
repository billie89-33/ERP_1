import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss'
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private cartService = inject(CartService);

  product: any = null;
  isLoading = true;
  error = '';
  
  quantity = 1;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadProduct(id);
    } else {
      this.error = 'Product ID is missing';
      this.isLoading = false;
    }
  }

  loadProduct(id: string) {
    this.productService.getProduct(id).subscribe({
      next: (res) => {
        this.product = res;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load product details.';
        this.isLoading = false;
      }
    });
  }

  getSpecsList(): {key: string, value: string}[] {
    if (!this.product || !this.product.specifications) return [];
    
    return Object.keys(this.product.specifications).map(key => ({
      key,
      value: this.product.specifications[key]
    }));
  }

  increaseQty() {
    if (this.quantity < this.product.stockQuantity) {
      this.quantity++;
    }
  }

  decreaseQty() {
    if (this.quantity > 1) {
      this.quantity--;
    }
  }

  addToCart() {
    if (this.product) {
      this.cartService.addToCart(this.product, this.quantity);
      // Optional: Reset quantity or show success toast
      this.quantity = 1;
    }
  }
}

