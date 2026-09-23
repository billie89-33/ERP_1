# Procurement UI & Data Integrity Design Specification

## 1. Data Integrity & Unique Constraints (การป้องกันข้อมูลซ้ำซ้อน)

To ensure strict data integrity across the ERP system, we will enforce `UNIQUE` constraints at both the Database (Entity Framework) and API validation levels.

### 1.1 Database Schema (EF Core)
In `AppDbContext.cs`, we will add Fluent API configurations to enforce uniqueness:

- **Supplier Entity:**
  - `CompanyName` -> **UNIQUE Index** (Prevent creating two vendors with the exact same name).
  - `TaxId` -> **UNIQUE Index** (Prevent registering the same legal entity twice).
- **Customer Entity:**
  - `CompanyName` -> **UNIQUE Index**.
  - `TaxId` -> **UNIQUE Index**.
- **Product Entity:**
  - `Sku` -> **UNIQUE Index** (Already partially enforced, will solidify).
- **Purchase Order / Sales Order:**
  - `PoNumber` / `OrderNumber` -> **UNIQUE Index**.

### 1.2 API Validation Logic
When `POST /api/Suppliers` is called:
1. Backend checks: `if (await _context.Suppliers.AnyAsync(s => s.CompanyName == dto.CompanyName || s.TaxId == dto.TaxId))`
2. If true, returns `400 Bad Request` with message: "ชื่อบริษัทหรือหมายเลขผู้เสียภาษีนี้มีอยู่ในระบบแล้ว"
3. The UI will catch this 400 error and display a red alert directly in the modal, preventing silent failures.

---

## 2. API Design (Backend)

### 2.1 Suppliers API
- `GET /api/Suppliers`
  - Purpose: Fetch list of suppliers for the dropdown.
  - Optimization: If data grows, implement `GET /api/Suppliers/search?q={query}` for lazy-loading autocomplete.
- `POST /api/Suppliers`
  - Payload: `{ companyName, contactName, email, phone, taxId, address }`
  - Purpose: Save a new supplier from the Quick Add Modal.

### 2.2 Goods Receipt API Update
- `POST /api/GoodsReceipts`
  - Current logic: Receives exact quantities from PO.
  - **New Logic:** Will accept an array of `ActualReceivedQty`.
  - Payload: `{ purchaseOrderId: Guid, items: [ { productId: Guid, receivedQuantity: int } ] }`
  - Updates `OnHandQuantity` based strictly on `receivedQuantity`.

---

## 3. Frontend UI & Logic (Angular)

### 3.1 PO List Component (`PurchaseOrderListComponent`)
- **Route:** `/purchase-orders`
- **Logic:** Calls `GET /api/PurchaseOrders` to display a data grid.
- **Features:** 
  - Status badges (Pending, Received).
  - Links to `/purchase-orders/:id` for detailed view and receiving goods.

### 3.2 Create PO Component (`PurchaseOrderFormComponent`)
- **Route:** `/purchase-orders/create`
- **Supplier Selection UX:**
  - **Autocomplete Dropdown:** Allows typing to search for existing suppliers (filtering the local array fetched from `GET /api/Suppliers`).
  - **Quick Add Button:** `[+ เพิ่มซัพพลายเออร์ใหม่]` located next to the dropdown.
- **Add Supplier Modal Logic:**
  - Opens a reactive form modal `(companyName, taxId, phone)`.
  - On submit, calls `POST /api/Suppliers`.
  - **Success:** Closes modal, re-fetches supplier list, and automatically selects the newly created supplier in the dropdown.
  - **Error:** Catches HTTP 400 (Unique Constraint violation) and displays the error message in the modal.
- **Line Items Grid:**
  - Replaces basic flex-rows with a HTML `<table>`.
  - Columns: Product (Dropdown), Qty, Unit Cost, Total (Auto-calculated).

### 3.3 Goods Receipt Component (`PurchaseOrderDetailComponent`)
- **Route:** `/purchase-orders/:id`
- **Logic:**
  - Displays PO details.
  - Renders a "Goods Receipt" table comparing `OrderedQty` vs `ActualReceivedQty` (Input field, defaults to OrderedQty).
  - On "Confirm Receipt", loops through the table and sends the payload to `POST /api/GoodsReceipts`.
  - Refreshes the page to show updated status (Completed) and locks the inputs.

---

## 4. Execution Plan (Step-by-Step)
1. **Database:** Add Unique Indexes in `AppDbContext.cs` and generate EF Migration.
2. **Backend APIs:** Update `SuppliersController` to handle uniqueness checks, and update `GoodsReceiptsController` to accept variable quantities.
3. **Frontend Routes & List:** Build `PurchaseOrderListComponent` and register routes.
4. **Frontend PO Form:** Implement the Table UI, Autocomplete Dropdown, and Quick Add Modal.
5. **Frontend GR Form:** Implement the physical quantity verification table.
