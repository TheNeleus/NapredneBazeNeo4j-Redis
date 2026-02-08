# 🌍 Meetup Clone App

Aplikacija za organizovanje događaja, bazirana na geolokaciji i real-time chatu.
Projekat za predmet: Napredne Baze Podataka.

## 🚀 Tehnologije

**Backend:**
* .NET 8 (C#)
* **Neo4j** (Graph Database) - Za čuvanje korisnika, događaja i relacija.
* **Redis** (Key-Value Store) - Za keširanje chat poruka (Hot Data) i Pub/Sub.
* SignalR - Za real-time komunikaciju.
* Docker - Za hostovanje baza.

**Frontend:**
* React + TypeScript (Vite)
* Leaflet (Mape)
* Axios & SignalR Client

---

## 🛠️ Kako pokrenuti projekat

### 1. Preduslovi
Potrebno je imati instalirano:
* [Docker Desktop](https://www.docker.com/)
* [Node.js](https://nodejs.org/)
* [.NET 8 SDK](https://dotnet.microsoft.com/)

### 2. Pokretanje Infrastrukture (Baze)

U root folderu projekta pokrenite:
```bash
docker-compose up -d
```

Ovo će podići Neo4j (port 7474/7687) i Redis (port 6379).

### 3. Pokretanje Backenda
Otvorite projekat1.sln u Visual Studiju.

Pritisnite Run (Play dugme) ili F5.

Backend će raditi na https://localhost:7000.

### 4. Pokretanje Frontenda
Otvorite terminal u folderu meetupfrontend:

Bash
cd meetupfrontend
npm install  #
npm run dev

Aplikacija će biti dostupna na http://localhost:5173