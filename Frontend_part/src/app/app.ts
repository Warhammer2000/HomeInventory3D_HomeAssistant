import {ChangeDetectionStrategy, Component, signal} from '@angular/core';
import {RouterOutlet, RouterLink, RouterLinkActive} from '@angular/router';
import {NgClass} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NgClass, MatIconModule],
  template: `
    <div class="flex h-screen w-full overflow-hidden bg-cream">
      <!-- Desktop Sidebar -->
      <aside class="hidden md:flex flex-col items-center w-[72px] hover:w-[240px] transition-all duration-300 ease-[cubic-bezier(0.34,1.56,0.64,1)] bg-surface shadow-soft z-50 group overflow-hidden shrink-0">
        <div class="flex items-center justify-center h-20 w-full shrink-0">
          <div class="w-10 h-10 squircle bg-accent text-white flex items-center justify-center font-heading font-bold text-xl">
            H
          </div>
        </div>
        
        <nav class="flex flex-col gap-4 mt-8 w-full px-3">
          @for (item of navItems; track item.path) {
            <a [routerLink]="item.path" 
               routerLinkActive="text-accent"
               #rla="routerLinkActive"
               class="relative flex items-center h-12 rounded-2xl group/nav hover:scale-[1.05] transition-transform duration-300 ease-[cubic-bezier(0.34,1.56,0.64,1)] cursor-pointer text-text-secondary">
              
              <!-- Active Indicator -->
              <div class="absolute inset-0 rounded-full transition-colors duration-300"
                   [ngClass]="rla.isActive ? 'bg-surface-secondary' : 'group-hover/nav:bg-gray-50'"></div>
              
              <div class="relative flex items-center w-full px-3">
                <mat-icon class="shrink-0" [ngClass]="rla.isActive ? 'text-accent' : ''">{{item.icon}}</mat-icon>
                <span class="ml-4 font-semibold whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity duration-300"
                      [ngClass]="rla.isActive ? 'text-accent' : ''">
                  {{item.label}}
                </span>
              </div>
            </a>
          }
        </nav>
      </aside>

      <!-- Main Content -->
      <main class="flex-1 h-full overflow-y-auto relative pb-20 md:pb-0">
        <!-- Blob Backgrounds -->
        <div class="fixed top-[-10%] left-[-5%] w-[40%] h-[40%] rounded-full bg-coral/5 blur-3xl -z-10 pointer-events-none"></div>
        <div class="fixed bottom-[-10%] right-[-5%] w-[50%] h-[50%] rounded-full bg-sky/5 blur-3xl -z-10 pointer-events-none"></div>
        
        <div class="max-w-7xl mx-auto w-full min-h-full">
          <router-outlet></router-outlet>
        </div>
      </main>

      <!-- Mobile Bottom Tab Bar -->
      <nav class="md:hidden fixed bottom-0 left-0 right-0 h-20 bg-surface shadow-[0_-8px_24px_rgba(45,42,50,0.06)] z-50 flex items-center justify-around px-2 pb-safe">
        @for (item of navItems; track item.path) {
          <a [routerLink]="item.path"
             routerLinkActive="text-accent"
             #rla="routerLinkActive"
             class="flex flex-col items-center justify-center w-16 h-14 gap-1 text-text-secondary transition-transform active:scale-95">
            <div class="w-8 h-8 squircle flex items-center justify-center transition-colors duration-300"
                 [ngClass]="rla.isActive ? 'bg-surface-secondary text-accent' : ''">
              <mat-icon class="text-[20px] w-5 h-5">{{item.icon}}</mat-icon>
            </div>
            <span class="text-[10px] font-medium" [ngClass]="rla.isActive ? 'text-accent' : ''">{{item.label}}</span>
          </a>
        }
      </nav>
    </div>
  `,
})
export class App {
  navItems = [
    { path: '/', icon: 'home', label: 'Главная' },
    { path: '/search', icon: 'search', label: 'Поиск' },
    { path: '/scan', icon: 'view_in_ar', label: 'Скан' },
    { path: '/settings', icon: 'settings', label: 'Настройки' },
  ];
}
