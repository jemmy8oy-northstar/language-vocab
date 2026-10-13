import { emptySplitApi as api } from "./emptyApi";
const injectedRtkApi = api.injectEndpoints({
  endpoints: (build) => ({
    getStatus: build.query<GetStatusApiResponse, GetStatusApiArg>({
      query: () => ({ url: `/api/status` }),
    }),
    getNextCard: build.query<GetNextCardApiResponse, GetNextCardApiArg>({
      query: (queryArg) => ({
        url: `/api/drill/next`,
        params: {
          direction: queryArg.direction,
        },
      }),
    }),
    answerCard: build.mutation<AnswerCardApiResponse, AnswerCardApiArg>({
      query: (queryArg) => ({
        url: `/api/drill/answer`,
        method: "POST",
        body: queryArg.answerRequest,
      }),
    }),
  }),
  overrideExisting: false,
});
export { injectedRtkApi as enhancedApi };
export type GetStatusApiResponse = unknown;
export type GetStatusApiArg = void;
export type GetNextCardApiResponse = /** status 200 OK */ IDrillCard;
export type GetNextCardApiArg = {
  direction?: DrillDirection;
};
export type AnswerCardApiResponse = /** status 200 OK */ IDrillOutcome;
export type AnswerCardApiArg = {
  answerRequest: AnswerRequest;
};
export type DrillDirection = "ZhToEn" | "EnToZh";
export type IDrillCard = {
  vocabItemId?: string;
  direction?: DrillDirection;
  hanzi?: null | string;
  pinyin?: null | string;
  glosses?: null | string[];
};
export type AnswerVerdict = "Wrong" | "Almost" | "Correct";
export type IDrillOutcome = {
  verdict?: AnswerVerdict;
  hanzi?: null | string;
  pinyin?: null | string;
  glosses?: null | string[];
  strength?: number | string;
  unlocked?: number | string;
};
export type AnswerRequest = {
  vocabItemId: string;
  direction: DrillDirection;
  answer: null | string;
};
export const { useGetStatusQuery, useGetNextCardQuery, useAnswerCardMutation } =
  injectedRtkApi;
