export enum SearchResultKind {
  Employee = 0,
  Post = 1,
  Community = 2,
  Document = 3,
  Event = 4,
}

export interface SearchResult {
  kind: SearchResultKind;
  id: string;
  title: string;
  subtitle: string | null;
  snippet: string | null;

  /** Built by the server, so the routes live in one place. */
  url: string;
  date: string | null;
}

export interface SearchResults {
  term: string;
  employees: SearchResult[];
  posts: SearchResult[];
  communities: SearchResult[];
  documents: SearchResult[];
  events: SearchResult[];
  totalShown: number;
}
