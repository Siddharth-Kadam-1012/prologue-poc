import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { AccountFormComponent } from './account-form.component';
import { CopyTemplateDialogComponent } from './copy-template-dialog.component';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    HttpClientModule,
    SharedModule,
    RouterModule.forChild([
      { path: '', component: AccountFormComponent },
      { path: 'copy', component: CopyTemplateDialogComponent }
    ])
  ],
  declarations: [AccountFormComponent, CopyTemplateDialogComponent]
})
export class AccountFormModule {}
