import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  Poll,
  SurveyAnswerInput,
  SurveyDetail,
  SurveyQuestion,
  SurveyQuestionType,
  SurveySummary,
} from '../../core/models/pulse';
import { LocaleService } from '../../core/services/locale.service';
import { PulseService } from '../../core/services/pulse.service';

/**
 * Polls and surveys in one place.
 *
 * A survey opens inline rather than on its own route: these are short instruments
 * people answer in a minute, and a navigation between the invitation and the first
 * question is where response rates go to die.
 */
@Component({
  selector: 'app-pulse-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  templateUrl: './pulse-page.html',
  styleUrl: './pulse-page.scss',
})
export class PulsePage {
  private readonly pulse = inject(PulseService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;
  readonly questionTypes = SurveyQuestionType;

  readonly polls = signal<Poll[]>([]);
  readonly surveys = signal<SurveySummary[]>([]);
  readonly loading = signal(true);

  readonly openSurvey = signal<SurveyDetail | null>(null);
  readonly answers = signal<Record<string, SurveyAnswerInput>>({});
  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly error = signal<string | null>(null);

  readonly isEmpty = computed(
    () => !this.loading() && this.polls().length === 0 && this.surveys().length === 0,
  );

  /** Required questions still unanswered - what disables the submit button. */
  readonly missingRequired = computed(() => {
    const survey = this.openSurvey();

    if (!survey) {
      return 0;
    }

    const current = this.answers();

    return survey.questions.filter(
      (question) => question.isRequired && !this.hasValue(current[question.id]),
    ).length;
  });

  constructor() {
    this.load();
  }

  vote(poll: Poll, optionId: string): void {
    if (!poll.canVote) {
      return;
    }

    // The server returns the poll as the caller may now see it, including the
    // tallies that voting has just earned them sight of - so the result replaces the
    // row rather than the client guessing at the new numbers.
    this.pulse.vote(poll.id, [optionId]).subscribe({
      next: (updated) =>
        this.polls.update((polls) => polls.map((item) => (item.id === poll.id ? updated : item))),
    });
  }

  share(poll: Poll, option: { votesCount: number | null }): number {
    const total = poll.totalVotes ?? 0;

    if (!total || option.votesCount === null) {
      return 0;
    }

    return Math.round((option.votesCount / total) * 100);
  }

  start(survey: SurveySummary): void {
    this.error.set(null);
    this.submitted.set(false);

    this.pulse.survey(survey.id).subscribe({
      next: (detail) => {
        this.openSurvey.set(detail);
        this.answers.set({});
      },
    });
  }

  close(): void {
    this.openSurvey.set(null);
    this.answers.set({});
  }

  setChoice(question: SurveyQuestion, optionId: string): void {
    this.patch(question.id, { optionIds: [optionId], textValue: null, numericValue: null });
  }

  setNumber(question: SurveyQuestion, value: number): void {
    this.patch(question.id, { optionIds: null, textValue: null, numericValue: value });
  }

  setText(question: SurveyQuestion, value: string): void {
    this.patch(question.id, { optionIds: null, textValue: value, numericValue: null });
  }

  chosen(questionId: string, optionId: string): boolean {
    return this.answers()[questionId]?.optionIds?.includes(optionId) ?? false;
  }

  numberFor(questionId: string): number | null {
    return this.answers()[questionId]?.numericValue ?? null;
  }

  textFor(questionId: string): string {
    return this.answers()[questionId]?.textValue ?? '';
  }

  /** The values a scale or NPS question offers, from its declared bounds. */
  scalePoints(question: SurveyQuestion): number[] {
    const min = question.questionType === SurveyQuestionType.Nps ? 0 : (question.minValue ?? 1);
    const max = question.questionType === SurveyQuestionType.Nps ? 10 : (question.maxValue ?? 5);

    return Array.from({ length: Math.max(0, max - min + 1) }, (_, index) => min + index);
  }

  submit(): void {
    const survey = this.openSurvey();

    if (!survey || this.submitting() || this.missingRequired() > 0) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const answers = Object.values(this.answers()).filter((answer) => this.hasValue(answer));

    this.pulse.respond(survey.id, answers).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
        this.openSurvey.set(null);
        this.load();
      },
      error: (response) => {
        this.submitting.set(false);
        this.error.set(response?.error?.message ?? this.text().pulse.submitFailed);
      },
    });
  }

  private hasValue(answer: SurveyAnswerInput | undefined): boolean {
    if (!answer) {
      return false;
    }

    return (
      (answer.optionIds?.length ?? 0) > 0 ||
      answer.numericValue !== null ||
      (answer.textValue ?? '').trim().length > 0
    );
  }

  private patch(questionId: string, value: Omit<SurveyAnswerInput, 'questionId'>): void {
    this.answers.update((current) => ({
      ...current,
      [questionId]: { questionId, ...value },
    }));
  }

  private load(): void {
    this.loading.set(true);

    this.pulse.polls().subscribe({
      next: (polls) => {
        this.polls.set(polls);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    this.pulse.surveys().subscribe({
      next: (surveys) => this.surveys.set(surveys),
      error: () => this.surveys.set([]),
    });
  }
}
