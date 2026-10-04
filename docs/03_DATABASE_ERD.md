# Database Architecture & ER Diagram
**Database Engine:** PostgreSQL (e.g., Supabase)
**Pattern:** Relational Database with JSONB Support for dynamic specs.

## 📊 ER Diagram (B2B ERP Version)

```mermaid
erDiagram
    USERS {
        Guid Id PK
        string Username
        string PasswordHash
        string Role "Admin, Sales, Purchasing"
    }
    
    CATEGORIES {
        Guid Id PK
        string Name
    }

    PRODUCTS {
        Guid Id PK
        string Sku UK
        string Name
        string Unit "หน่วยนับ เช่น Kg, Pack, Box"
        decimal Price "ราคามาตรฐาน"
        decimal Cost "ต้นทุน"
        int StockQuantity "จำนวนสต๊อกรวม (แบบง่าย)"
        jsonb Specifications "JSON: เช่น Origin, Organic, Frozen"
        Guid CategoryId FK
    }

    CUSTOMERS {
        Guid Id PK
        string CompanyName "ชื่อบริษัท/องค์กร (ลูกค้า B2B)"
        string TaxId "เลขประจำตัวผู้เสียภาษี"
        int CreditTermDays "เครดิตเทอม"
        decimal CreditLimit "วงเงินเครดิต"
        string Address
    }

    SUPPLIERS {
        Guid Id PK
        string CompanyName "ชื่อฟาร์ม/โรงงาน"
        string ContactName
        string Phone
    }

    QUOTATIONS {
        Guid Id PK
        string QuoteNumber UK
        DateTime QuoteDate
        DateTime ValidUntil
        string Status "Draft, Sent, Accepted"
        decimal TotalAmount
        Guid CustomerId FK
    }

    QUOTATION_ITEMS {
        Guid Id PK
        int Quantity
        decimal UnitPrice
        decimal Discount
        Guid QuotationId FK
        Guid ProductId FK
    }

    SALES_ORDERS {
        Guid Id PK
        string OrderNumber UK
        DateTime OrderDate
        DateTime ExpectedDeliveryDate "วันกำหนดส่งมอบสินค้า"
        string Status "Pending, Shipped, Delivered"
        decimal TotalAmount
        Guid CustomerId FK
        Guid CreatedByUserId FK
    }

    SALES_ORDER_ITEMS {
        Guid Id PK
        int Quantity
        decimal UnitPrice
        Guid SalesOrderId FK
        Guid ProductId FK
    }

    PURCHASE_ORDERS {
        Guid Id PK
        string PoNumber UK
        DateTime OrderDate
        DateTime ExpectedReceiveDate "วันนัดฟาร์มมาส่งของ"
        string Status "Pending, Received"
        decimal TotalAmount
        Guid SupplierId FK
        Guid CreatedByUserId FK
    }

    PURCHASE_ORDER_ITEMS {
        Guid Id PK
        int Quantity
        decimal UnitCost
        Guid PurchaseOrderId FK
        Guid ProductId FK
    }

    %% Relationships
    CATEGORIES ||--o{ PRODUCTS : "has"
    PRODUCTS ||--o{ QUOTATION_ITEMS : "included in"
    PRODUCTS ||--o{ SALES_ORDER_ITEMS : "included in"
    PRODUCTS ||--o{ PURCHASE_ORDER_ITEMS : "ordered in"
    
    CUSTOMERS ||--o{ QUOTATIONS : "requests"
    QUOTATIONS ||--|{ QUOTATION_ITEMS : "contains"

    CUSTOMERS ||--o{ SALES_ORDERS : "places"
    SALES_ORDERS ||--|{ SALES_ORDER_ITEMS : "contains"
    USERS ||--o{ SALES_ORDERS : "creates"

    SUPPLIERS ||--o{ PURCHASE_ORDERS : "receives"
    PURCHASE_ORDERS ||--|{ PURCHASE_ORDER_ITEMS : "contains"
    USERS ||--o{ PURCHASE_ORDERS : "creates"
```

## 📝 แนวทางการจัดการข้อมูล
1. **Primary Key:** ทุกตารางใช้ UUID/Guid ป้องกันปัญหา ID ชนกันเมื่อระบบขยายตัว
2. **JSONB (Specifications):** ใช้ข้อได้เปรียบของ PostgreSQL ในการเก็บโครงสร้าง Data ที่ไม่มี Schema ตายตัวแบบที่เคยทำใน MongoDB
3. **Data Integrity:** ใช้ Physical Foreign Key (FK) 90% ของระบบ เพื่อความถูกต้องของบัญชีและสต๊อก และจะปลด FK (ใช้ Loose Coupling) เฉพาะกรณีตารางที่ซับซ้อนมากหรือทำ Microservices เท่านั้น
