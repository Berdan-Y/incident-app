# My Application

A cross-platform application with Blazor, API, and MAUI (Android) projects.

## 📦 Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/en-us/download) (version matching your projects)
- [JetBrains Rider](https://www.jetbrains.com/rider/)
- Android SDK (for MAUI project)
- Azure Data Studio (for database management)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Berdan-Y/incident-app
cd incident-app
```

### 2. Open in Solution
Open the solution in JetBrains Rider. You should see the following projects:

Blazor Project

API Project

MAUI (Android) Project

### 3. Configure Run Settings in Rider
Go to Run > Edit Configurations..., and add the following:

🔹 Blazor App
Type: ```.NET Launch Settings Profile```

Project: Blazor

🔹 API Backend
Type: ```.NET Launch Settings Profile```

Project: API

🔹 Android App (MAUI)
Type: ```Android```

Project: MAUI

### 4. Set Up Database
Ensure your database is set up and accessible. You can use Azure Data Studio to manage your database.
The credentials and connection strings are available in the `appsettings.json` files of the API projects. (The database and GOOGLE API keys used in this project will be deleted after all assessments are completed
with a passing grade.)

### 5. Run the Projects
Start all three configurations. Once running, the application is ready to use.