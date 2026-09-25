# 🏢 Jamine ERP & B2C Storefront

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Backend](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Frontend](https://img.shields.io/badge/Angular-18-DD0031?logo=angular)
![Database](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![Styling](https://img.shields.io/badge/Tailwind_CSS-3.4-38B2AC?logo=tailwind-css)

**Jamine ERP** คือระบบจัดการทรัพยากรองค์กร (Enterprise Resource Planning) แบบ Full-Stack ที่มาพร้อมกับหน้าเว็บขายสินค้าแบบ B2C (Storefront) ระบบถูกออกแบบมาให้ทำงานร่วมกันอย่างลื่นไหล ตั้งแต่ลูกค้ากดสั่งซื้อหน้าเว็บ ไปจนถึงการเปิดบิล ตัดสต๊อก และส่งของในระบบหลังบ้าน

---

## ✨ Features (ความสามารถหลัก)

### 🛒 1. B2C E-Commerce Storefront (หน้าร้าน)
*   **Product Catalog:** แสดงรายการสินค้าพร้อมรูปภาพ, ราคา, และสถานะสต๊อกแบบ Real-time
*   **Shopping Cart & Checkout:** ระบบตะกร้าสินค้า และการสั่งซื้อด้วยตัวเอง (Self-checkout)
*   **Customer Portal:** หน้าต่าง "บัญชีของฉัน" สำหรับลูกค้าเพื่อติดตามสถานะคำสั่งซื้อ (Order Tracking) และอัปโหลดสลิปโอนเงิน

### 💼 2. ERP Back-Office (ระบบจัดการหลังบ้าน)
*   **Master Data:** จัดการข้อมูลลูกค้า (Customers), ซัพพลายเออร์ (Suppliers), สินค้า (Products) และผู้ใช้งานระบบ (Users)
*   **Sales Module:** จัดการใบเสนอราคา (Quotation) และใบสั่งขาย (Sales Order) อนุมัติสลิปโอนเงิน
*   **Purchasing Module:** จัดการใบสั่งซื้อ (Purchase Order) ไปยังซัพพลายเออร์
*   **Warehouse / Inventory:** ตัดสต๊อกแบบ Real-time ผ่านการรับของ (Goods Receipt - GR) และการส่งมอบของ (Goods Issue - GI)
*   **Dashboard & Analytics:** กระดานสรุปยอดขาย, ต้นทุน, มูลค่าสต๊อกคงเหลือ, และสินค้าขายดี
*   **Print & Export:** ระบบพิมพ์ใบเสร็จ/ใบกำกับภาษี, ใบสั่งซื้อ (PDF) และ Export ข้อมูลเป็น Excel

---

## 🛠️ Tech Stack (เทคโนโลยีที่ใช้)

*   **Frontend:** Angular 18 (Standalone Components), Tailwind CSS, RxJS, Chart.js
*   **Backend:** ASP.NET Core 8 Web API, C#
*   **Database:** PostgreSQL (ผ่าน Entity Framework Core / LINQ)
*   **Authentication:** JWT (JSON Web Tokens) ควบคู่กับ HTTP-only Cookies
*   **Other Tools:** ClosedXML (สำหรับ Export Excel)

---

## 🚀 Getting Started (วิธีติดตั้งและใช้งาน)

### Prerequisites (สิ่งที่ต้องมี)
*   [Node.js](https://nodejs.org/) (v18+)
*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [PostgreSQL](https://www.postgresql.org/) (v14+)

### 1. Database Setup
ระบบจะอ่าน Connection String จากไฟล์ `.env` ที่อยู่ในโฟลเดอร์ Root (`E:\Jamine_ERP\.env`)
ให้สร้างไฟล์ `.env` โดยยึดตามรูปแบบจาก `.env.example`:
```env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=jamine_erp_db
DB_USER=postgres
DB_PASSWORD=your_password
JWT_KEY=your_super_secret_jwt_key_that_is_at_least_32_bytes_long
```

### 2. Backend Setup
เปิด Terminal แล้วเข้าไปที่โฟลเดอร์ `backend`:
```bash
cd backend
# อัปเดต Database Schema (Migrations)
dotnet ef database update
# รันเซิร์ฟเวอร์ (API จะทำงานที่ http://localhost:5243)
dotnet run
```

### 3. Frontend Setup
เปิด Terminal หน้าต่างใหม่ แล้วเข้าไปที่โฟลเดอร์ `frontend`:
```bash
cd frontend
# ติดตั้ง Packages
npm install
# รันเซิร์ฟเวอร์ (เว็บจะทำงานที่ http://localhost:4200)
npm start
```

---

## 🔐 Default Admin Account
หลังจากติดตั้งระบบและสร้างฐานข้อมูลสำเร็จ ระบบจะสร้างบัญชีแอดมินให้โดยอัตโนมัติ:
*   **Username:** `admin`
*   **Password:** `password` (หรือ `admin123`)

---

## 📂 Project Structure (โครงสร้างโปรเจกต์)
```text
Jamine_ERP/
â”œâ”€â”€ backend/               # ASP.NET Core Web API
â”‚   â”œâ”€â”€ Controllers/       # API Endpoints
â”‚   â”œâ”€â”€ Models/            # Database Entities
â”‚   â”œâ”€â”€ Data/              # Entity Framework DbContext
â”‚   â””â”€â”€ Services/          # Business Logic & Auth Services
â”‚
â”œâ”€â”€ frontend/              # Angular 18 Application
â”‚   â”œâ”€â”€ src/
â”‚   â”‚   â”œâ”€â”€ app/
â”‚   â”‚   â”‚   â”œâ”€â”€ core/      # Guards, Interceptors, Models
â”‚   â”‚   â”‚   â”œâ”€â”€ features/  # Business Modules (Sales, Purchasing, Warehouse, Storefront)
â”‚   â”‚   â”‚   â””â”€â”€ layout/    # Admin Sidebar, Storefront Navbar
â”‚   â”‚   â””â”€â”€ styles.scss    # Global Styles & Tailwind Configuration
â”‚
â””â”€â”€ docs/                  # Project Design & Architecture Documentation
```
