# 🧾 POS System - Backend (.NET)

## 📌 Overview

This project is a **Point of Sale (POS) Backend System** built using **.NET (ADO.NET)** with a clean layered architecture.

It manages:

* Products & Categories
* Customers
* Invoices (Sales)
* Stock Management (Inventory)

The system is designed with **performance, scalability, and data integrity** in mind.

---

## 🏗️ Architecture

The project follows a **3-Layer Architecture**:

```
API Layer (Controllers)
↓
Business Layer (Services)
↓
Data Access Layer (Repositories + Stored Procedures)
↓
SQL Server Database
```

---

## 🚀 Key Features

### 🧾 Invoice Management

* Create full invoice in **one atomic operation**
* Handles:

  * Invoice creation
  * Invoice details
  * Stock deduction
  * Stock movement logging

### 📦 Stock Management

* Automatic stock updates on sales
* Full **Stock Movement History**
* Prevents overselling using:

  * Transactions
  * SQL Locks (UPDLOCK, HOLDLOCK)

### 🗂️ Category & Product Management

* CRUD operations
* Soft delete support
* Pagination

### 👤 Customer Management

* Manage customers and balances

---

## 💣 Core Highlight (Important)

### 🔥 `CreateInvoiceFullFlow`

A single stored procedure that:

```
✔️ Creates Invoice
✔️ Inserts Invoice Details
✔️ Updates Stock
✔️ Inserts Stock Movements
✔️ Calculates Total
✔️ Executes in ONE Transaction
```

👉 This ensures:

* No data inconsistency
* No race conditions
* High performance

---

## 🧠 Technologies Used

* .NET Web API
* ADO.NET
* SQL Server
* Stored Procedures
* Serilog (Logging)

---

## 🛠️ Database Design

Key Tables:

* `Products`
* `Categories`
* `Customers`
* `Invoices`
* `InvoiceDetails`
* `StockMovements`

---

## 🔐 Security

> ⚠️ Security (Authentication & Authorization) **will be added later**

Planned:

* JWT Authentication
* Role-based Authorization (Admin / Cashier)

---

## 📊 Future Improvements

* 🔐 Authentication & Authorization
* 📈 Sales Reports & Dashboard
* 💰 Profit Calculation
* 🧾 Invoice Printing (PDF)
* ⚡ Caching (Redis)
* 🧪 Unit Testing

---

## ⚙️ API Examples

### Create Full Invoice

```
POST /api/invoices/CreateFullInvoice
```

### Get Invoice With Details

```
GET /api/invoices/GetInvoiceWithDetails/{id}
```

### Get Stock Movements

```
GET /api/StockMovements/GetByProductId/{id}
```

---

## 🧪 Notes

* All critical operations use **Transactions**
* Business logic is handled in **Service Layer**
* Database enforces **data integrity**

---

## 👨‍💻 Author

Developed as part of a professional backend engineering practice project.
