import {ChangeDetectionStrategy, Component} from '@angular/core';
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
      <aside class="hidden md:flex flex-col items-center w-[240px] bg-surface shadow-soft z-50 shrink-0">
        <div class="flex items-center gap-3 h-20 w-full px-6 shrink-0">
          <div class="w-10 h-10 squircle bg-accent text-white flex items-center justify-center font-heading font-bold text-xl">
            H
          </div>
          <span class="font-heading font-bold text-lg text-text-primary">Inventory</span>
        </div>

        <nav class="flex flex-col gap-2 mt-4 w-full px-4">
          @for (item of navItems; track item.path) {
            <a [routerLink]="item.path"
               routerLinkActive="active-nav"
               #rla="routerLinkActive"
               class="flex items-center gap-4 h-12 px-4 rounded-2xl cursor-pointer transition-colors duration-200 text-text-secondary hover:bg-gray-50"
               [ngClass]="rla.isActive ? 'bg-surface-secondary text-accent' : ''">
              <mat-icon [ngClass]="rla.isActive ? 'text-accent' : ''">{{item.icon}}</mat-icon>
              <span class="font-semibold text-sm" [ngClass]="rla.isActive ? 'text-accent' : ''">{{item.label}}</span>
            </a>
          }
        </nav>
      </aside>

      <!-- Main Content -->
      <main class="flex-1 h-full overflow-y-auto relative pb-20 md:pb-0">
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
