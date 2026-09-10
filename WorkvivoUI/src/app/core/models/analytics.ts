export interface Metric {
  key: string;
  value: number;
  previous: number | null;
}

export interface TopPost {
  id: string;
  title: string;
  authorDisplayName: string;
  views: number;
  reactions: number;
  comments: number;
  publishedAt: string | null;
}

export interface ActivityPoint {
  date: string;
  posts: number;
  comments: number;
  reactions: number;
}

export interface AnalyticsDashboard {
  days: number;
  generatedAt: string;
  metrics: Metric[];
  topPosts: TopPost[];
  activity: ActivityPoint[];
}
