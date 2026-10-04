import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { StorefrontAuthService } from '../../../core/services/storefront-auth.service';

@Component({
  selector: 'app-customer-portal',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './customer-portal.component.html'
})
export class CustomerPortalComponent implements OnInit {
  authService = inject(StorefrontAuthService);
  http = inject(HttpClient);
  router = inject(Router);
  fb = inject(FormBuilder);

  orders: any[] = [];
  isLoading = true;
  isSavingProfile = false;
  profileSaveSuccess = false;
  isEditMode = false;
  profileData: any = null;

  profileForm: FormGroup = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: ['', Validators.required],
    address: ['', Validators.required]
  });

  ngOnInit() {
    if (!this.authService.currentUser()) {
      this.router.navigate(['/shop/login']);
      return;
    }
    this.fetchProfile();
    this.fetchOrders();
  }

  fetchProfile() {
    this.http.get<any>('/api/StorefrontAuth/profile', { withCredentials: true }).subscribe({
      next: (data) => {
        this.profileData = data;
        this.profileForm.patchValue({
          firstName: data.firstName || '',
          lastName: data.lastName || '',
          phone: data.phone || '',
          address: data.address || ''
        });
        // Auto enter edit mode if profile is completely empty
        if (!data.phone && !data.address) {
           this.isEditMode = true;
        }
      },
      error: (err) => console.error('Error loading profile', err)
    });
  }

  toggleEditMode() {
    this.isEditMode = !this.isEditMode;
    if (this.isEditMode && this.profileData) {
      this.profileForm.patchValue({
        firstName: this.profileData.firstName || '',
        lastName: this.profileData.lastName || '',
        phone: this.profileData.phone || '',
        address: this.profileData.address || ''
      });
    }
  }

  saveProfile() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    this.isSavingProfile = true;
    this.profileSaveSuccess = false;

    this.http.put('/api/StorefrontAuth/profile', this.profileForm.value, { withCredentials: true }).subscribe({
      next: () => {
        this.isSavingProfile = false;
        this.profileSaveSuccess = true;
        this.isEditMode = false;
        this.profileData = {
          ...this.profileData,
          ...this.profileForm.value
        };
        setTimeout(() => this.profileSaveSuccess = false, 3000);
        
        // update local signal if needed
        const current = this.authService.currentUser();
        if (current) {
           this.authService.currentUser.set({
              ...current,
              firstName: this.profileForm.value.firstName,
              lastName: this.profileForm.value.lastName
           });
        }
      },
      error: (err) => {
        this.isSavingProfile = false;
        console.error('Error saving profile', err);
      }
    });
  }

  fetchOrders() {
    this.http.get<any[]>('/api/Storefront/my-orders', { withCredentials: true }).subscribe({
      next: (data) => {
        this.orders = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  logout() {
    this.authService.logout().subscribe(() => {
      this.router.navigate(['/']);
    });
  }
}
