import { Component, ElementRef, ViewChild, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { FeaturedProductDto } from '../../../core/models/erp.model';
import { ProductDto } from '../../../core/models/product.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  @ViewChild('scrollContainer') scrollContainer!: ElementRef;
  
  productService = inject(ProductService);
  cartService = inject(CartService);
  newProducts: FeaturedProductDto[] = [];
  isLoading = true;

  ngOnInit() {
    this.productService.getFeaturedProducts().subscribe({
      next: (products) => {
        this.newProducts = products;
        this.isLoading = false;
      },
      error: (err) => {
        console.error("Failed to load featured products", err);
        this.isLoading = false;
      }
    });
  }

  scrollLeft() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollBy({ left: -300, behavior: 'smooth' });
    }
  }

  addToCart(event: Event, product: FeaturedProductDto) {
    event.preventDefault();
    event.stopPropagation();
    const cartProduct: ProductDto = {
      id: product.id,
      sku: '',
      name: product.name,
      brand: '',
      modelName: '',
      description: '',
      price: product.price,
      cost: 0,
      onHandQuantity: 99,
      reservedQuantity: 0,
      availableQuantity: 99,
      categoryId: '',
      categoryName: product.categoryName,
      image: { url: product.imageUrl || '', publicId: '' },
      tags: [],
      status: 'Active',
      isFeatured: true,
      soldCount: 0,
      viewCount: 0
    };
    this.cartService.addToCart(cartProduct, 1);
    alert(product.name + ' ถูกเพิ่มลงตะกร้าแล้ว!');
  }

  scrollRight() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollBy({ left: 300, behavior: 'smooth' });
    }
  }
}
