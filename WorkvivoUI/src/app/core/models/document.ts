export interface DocumentCategory {
  id: string;
  name: string;
  icon: string | null;
  documentCount: number;
}

export interface DocumentSummary {
  id: string;
  categoryId: string;
  categoryName: string;
  title: string;
  description: string | null;
  fileName: string | null;
  contentType: string | null;
  sizeBytes: number | null;
  versionNumber: number;
  downloadCount: number;
  publishedAt: string | null;
  reviewDate: string | null;
  ownerDisplayName: string;
  isDownloadable: boolean;
  canManage: boolean;
}
