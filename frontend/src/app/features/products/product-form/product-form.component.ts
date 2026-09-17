import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  productForm: FormGroup;
  isEditMode = false;
  productId: string | null = null;
  categories: any[] = [];
  specificationsText = '{\n  "RAM": "16GB",\n  "Storage": "512GB SSD"\n}';
  specError = '';
  
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);

  constructor() {
    this.productForm = this.fb.group({
      sku: ['', Validators.required],
      name: ['', Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      cost: [0, [Validators.required, Validators.min(0)]],
      stockQuantity: [0, [Validators.required, Validators.min(0)]],
      categoryId: ['', Validators.required],
    });
  }

  ngOnInit() {
    this.loadCategories();
    this.productId = this.route.snapshot.paramMap.get('id');
    
    if (this.productId) {
      this.isEditMode = true;
      this.productService.getProduct(this.productId).subscribe(product => {
        this.productForm.patchValue({
          sku: product.sku,
          name: product.name,
          price: product.price,
          cost: product.cost,
          stockQuantity: product.stockQuantity,
          categoryId: product.categoryId
        });
        if (product.specifications) {
          this.specificationsText = JSON.stringify(product.specifications, null, 2);
        }
      });
    }
  }

  loadCategories() {
    // For now, call direct API or mock
    this.http.get<any[]>('http://localhost:5000/api/categories').subscribe(
      res => this.categories = res,
      err => {
        // Fallback mock if category table is empty or api fails
        this.categories = [
          { id: '11111111-1111-1111-1111-111111111111', name: 'Laptops (Mock)' },
          { id: '22222222-2222-2222-2222-222222222222', name: 'PCs (Mock)' }
        ];
      }
    );
  }

  onSubmit() {
    if (this.productForm.invalid) return;

    let specsObj = null;
    this.specError = '';
    
    if (this.specificationsText.trim()) {
      try {
        specsObj = JSON.parse(this.specificationsText);
      } catch (e) {
        this.specError = 'Invalid JSON format in specifications. Please fix before submitting.';
        return;
      }
    }

    const payload = {
      ...this.productForm.value,
      specifications: specsObj
    };

    if (this.isEditMode && this.productId) {
      this.productService.updateProduct(this.productId, payload).subscribe({
        next: () => this.router.navigate(['/admin/products']),
        error: (err) => alert(err.error?.message || 'Error updating product')
      });
    } else {
      this.productService.createProduct(payload).subscribe({
        next: () => this.router.navigate(['/admin/products']),
        error: (err) => alert(err.error?.message || 'Error creating product')
      });
    }
  }
}
