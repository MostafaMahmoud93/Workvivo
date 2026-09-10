export enum EventFormat {
  Physical = 0,
  Online = 1,
  Hybrid = 2,
}

export enum EventStatus {
  Draft = 0,
  Published = 1,
  Cancelled = 2,
  Completed = 3,
}

export enum EventResponse {
  Attending = 0,
  Maybe = 1,
  Declined = 2,
}

export interface CalendarEvent {
  id: string;
  title: string;
  description: string | null;
  eventType: number;
  format: EventFormat;
  status: EventStatus;
  startAt: string;
  endAt: string;
  timeZoneId: string | null;
  isAllDay: boolean;
  address: string | null;
  locationName: string | null;
  organizerEmployeeId: string;
  organizerDisplayName: string | null;
  attendeesCount: number;
  capacity: number | null;
  requiresRsvp: boolean;
  myResponse: EventResponse | null;
  canRsvp: boolean;

  /** Present only once you are attending - the server withholds it otherwise. */
  meetingUrl: string | null;
}
