# Expense Tracker API

Μια RESTful Web API εφαρμογή για την παρακολούθηση και διαχείριση εξόδων (Expense Tracker). Η εφαρμογή είναι αναπτυγμένη με **ASP.NET Core** και **Entity Framework Core**, και φιλοξενείται στο **Microsoft Azure** χρησιμοποιώντας **Azure SQL Database**.

## Βασικά Χαρακτηριστικά (Features)
* **Διαχείριση Χρηστών & Πιστοποίηση (Authentication):** Ασφαλής εγγραφή και σύνδεση χρηστών (`AuthController`).
* **Διαχείριση Εξόδων (Expense Management):** Προσθήκη, προβολή, επεξεργασία και διαγραφή εξόδων (`ExpensesController`).
* **Telegram Bot Integration:** Διασύνδεση με το Telegram για εύκολη καταγραφή και ενημέρωση εξόδων μέσω μηνυμάτων (`TelegramController`).
* **Cloud-Native:** Πλήρως λειτουργικό στο cloud, ρυθμισμένο για Azure App Service και Azure SQL.

## Τεχνολογίες (Tech Stack)
* **Framework:** C# / ASP.NET Core (.NET)
* **ORM:** Entity Framework Core
* **Βάση Δεδομένων:** Azure SQL Database
* **Hosting:** Azure Web App (App Service)

## Ανάπτυξη & Παραγωγή (Deployment)
Το project είναι αυτή τη στιγμή deployed και τρέχει κανονικά στο **Microsoft Azure**. 
* Η βάση δεδομένων διαχειρίζεται μέσω της **Azure SQL Database**.
* Οι αλλαγές στο σχήμα της βάσης γίνονται μέσω των Entity Framework Core Migrations.
