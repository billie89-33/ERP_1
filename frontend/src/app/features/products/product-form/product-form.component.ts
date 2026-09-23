import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  productForm: FormGroup;
  isEditMode = false;
  productId: string | null = null;
  categories: any[] = [];
  
  // Dynamic Specs Array instead of raw text
  dynamicSpecs: { key: string, value: string }[] = [
    { key: 'RAM', value: '16GB DDR5' }, // Default Example
    { key: 'Storage', value: '1TB SSD' }
  ];
  
  imageUrl: string | null = null;
  isUploadingImage = false;
  
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);

  constructor() {
    this.productForm = this.fb.group({
      sku: ['', Validators.required],
      name: ['', Validators.required],
      brand: ['', Validators.required],
      modelName: ['', Validators.required],
      description: [''],
      price: [0, [Validators.required, Validators.min(0)]],
      cost: [0, [Validators.required, Validators.min(0)]],
      categoryId: ['', Validators.required],
      tags: [''],
      status: ['ACTIVE'],
      isFeatured: [false]
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
          categoryId: product.categoryId,
          brand: product.brand,
          modelName: product.modelName,
          description: product.description,
          tags: product.tags ? product.tags.join(', ') : '',
          status: product.status,
          isFeatured: product.isFeatured
        });
        
        this.imageUrl = product.image?.url || null;
        this.cloudinaryPublicId = product.image?.publicId || null;

        // Parse JSON object back into dynamic array
        if (product.specifications && typeof product.specifications === 'object') {
          this.dynamicSpecs = [];
          for (const [key, value] of Object.entries(product.specifications)) {
            this.dynamicSpecs.push({ key, value: String(value) });
          }
        } else {
            this.dynamicSpecs = []; // Empty if no specs
        }
      });
    }
  }

  // --- Dynamic Specs Helpers ---
  addSpecRow() {
    this.dynamicSpecs.push({ key: '', value: '' });
  }

  removeSpecRow(index: number) {
    this.dynamicSpecs.splice(index, 1);
  }
  // -----------------------------

  cloudinaryPublicId: string | null = null;

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (!file) return;

    this.isUploadingImage = true;

    if (this.productId) {
      // Edit Mode
      this.productService.uploadProductImage(this.productId, file).subscribe({
        next: (res) => {
          this.imageUrl = res.imageUrl;
          this.isUploadingImage = false;
        },
        error: (err) => {
          alert('Failed to upload image: ' + (err.error?.message || err.message));
          this.isUploadingImage = false;
        }
      });
    } else {
      // Create Mode (Upload temp image)
      const formData = new FormData();
      formData.append('file', file);
      
      this.http.post<any>('http://localhost:5243/api/products/upload-temp-image', formData).subscribe({
        next: (res) => {
          this.imageUrl = res.imageUrl;
          this.cloudinaryPublicId = res.publicId;
          this.isUploadingImage = false;
        },
        error: (err) => {
          alert('Failed to upload image: ' + (err.error?.message || err.message));
          this.isUploadingImage = false;
        }
      });
    }
  }

  loadCategories() {
    // For now, call direct API or mock
    this.http.get<any[]>('http://localhost:5243/api/categories').subscribe(
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

    // Convert Dynamic Array back to JSON Object for backend
    const specsObj: any = {};
    for (const spec of this.dynamicSpecs) {
      if (spec.key.trim() !== '') {
        specsObj[spec.key.trim()] = spec.value.trim();
      }
    }

    const payload = {
      ...this.productForm.value,
      specifications: Object.keys(specsObj).length > 0 ? specsObj : null,
      image: {
        url: this.imageUrl || '',
        publicId: this.cloudinaryPublicId || ''
      },
      tags: this.productForm.value.tags ? this.productForm.value.tags.split(',').map((t: string) => t.trim()).filter((t: string) => t) : []
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
