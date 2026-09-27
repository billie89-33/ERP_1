import { Component, ElementRef, ViewChild, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../../core/services/product.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  @ViewChild('scrollContainer') scrollContainer!: ElementRef;
  
  productService = inject(ProductService);
  newProducts: any[] = [];
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

  scrollRight() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollBy({ left: 300, behavior: 'smooth' });
    }
  }
}
