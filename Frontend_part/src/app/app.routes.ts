import {Routes} from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'container/:id',
    loadComponent: () => import('./pages/container-detail.component').then(m => m.ContainerDetailComponent)
  },
  {
    path: 'scan',
    loadComponent: () => import('./pages/scan.component').then(m => m.ScanComponent)
  },
  {
    path: 'search',
    loadComponent: () => import('./pages/search.component').then(m => m.SearchComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./pages/home.component').then(m => m.HomeComponent) // Placeholder
  }
];
