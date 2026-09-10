export enum PollStatus {
  Draft = 0,
  Open = 1,
  Closed = 2,
}

export enum SurveyQuestionType {
  SingleChoice = 0,
  MultipleChoice = 1,
  Rating = 2,
  Text = 3,
  YesNo = 4,
  Nps = 5,
  Scale = 6,
}

export interface PollOption {
  id: string;
  text: string;
  sortOrder: number;

  /** Null while results are withheld - absent, not zero. */
  votesCount: number | null;
  chosenByMe: boolean;
}

export interface Poll {
  id: string;
  postId: string | null;
  question: string;
  isMultipleChoice: boolean;
  isAnonymous: boolean;
  status: PollStatus;
  expiryDate: string | null;
  totalVotes: number | null;
  hasVoted: boolean;
  canVote: boolean;
  resultsVisible: boolean;
  options: PollOption[];
}

export interface SurveyQuestionOption {
  id: string;
  text: string;
  sortOrder: number;
}

export interface SurveyQuestion {
  id: string;
  questionType: SurveyQuestionType;
  text: string;
  helpText: string | null;
  isRequired: boolean;
  sortOrder: number;
  minValue: number | null;
  maxValue: number | null;
  minLabel: string | null;
  maxLabel: string | null;
  options: SurveyQuestionOption[];
}

export interface SurveySummary {
  id: string;
  title: string;
  description: string | null;
  status: number;
  isAnonymous: boolean;
  endDate: string | null;
  hasResponded: boolean;
  canRespond: boolean;
  questionCount: number;
}

export interface SurveyDetail extends SurveySummary {
  questions: SurveyQuestion[];
}

export interface SurveyAnswerInput {
  questionId: string;
  optionIds: string[] | null;
  textValue: string | null;
  numericValue: number | null;
}
