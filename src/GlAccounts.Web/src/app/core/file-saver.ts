import { Injectable } from '@angular/core';
import { DownloadedFile } from './models';

/** Starts a browser download for a file the API returned. */
@Injectable({ providedIn: 'root' })
export class FileSaver {
  save(file: DownloadedFile): void {
    const url = URL.createObjectURL(file.blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = file.fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}
