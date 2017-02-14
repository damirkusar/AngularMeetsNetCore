import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { UniversalModule } from 'angular2-universal';
import { FormsModule } from '@angular/forms';

import { LocalizationModule } from 'angular-l10n';

import { AdminComponent } from './admin/admin.component';

import { AuthGuardService } from '../../services/authGuard.service';
import { CanDeactivateGuardService } from '../../services/canDeactivateGuard.service';

@NgModule({
    declarations: [
        AdminComponent
    ],
    imports: [
        // Angular Modules
        UniversalModule, // Must be first import. This automatically imports BrowserModule, HttpModule, and JsonpModule too.
        FormsModule,
        RouterModule.forChild([
            {
                path: 'admin',
                component: AdminComponent,
                data: { auth: true, roles: ['admin'] },
                canActivate: [AuthGuardService],
                canDeactivate: [CanDeactivateGuardService],
                canActivateChild: [AuthGuardService],
                children: []
            }
        ]),
        LocalizationModule.forChild()
        // My Modules
    ],
    exports: [AdminComponent],
    providers: [
    ]
})
export class AdminModule {
}