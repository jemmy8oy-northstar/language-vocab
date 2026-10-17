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
    getLearnRound: build.query<GetLearnRoundApiResponse, GetLearnRoundApiArg>({
      query: (queryArg) => ({
        url: `/api/drill/learn`,
        params: {
          count: queryArg.count,
          pack: queryArg.pack,
        },
      }),
    }),
    getPacks: build.query<GetPacksApiResponse, GetPacksApiArg>({
      query: () => ({ url: `/api/packs` }),
    }),
    getStats: build.query<GetStatsApiResponse, GetStatsApiArg>({
      query: () => ({ url: `/api/stats` }),
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
export type GetLearnRoundApiResponse = /** status 200 OK */ ILearnCard[];
export type GetLearnRoundApiArg = {
  count?: number;
  pack?: string;
};
export type GetPacksApiResponse = /** status 200 OK */ IPack[];
export type GetPacksApiArg = void;
export type GetStatsApiResponse = /** status 200 OK */ IStatsReport;
export type GetStatsApiArg = void;
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
export type ILearnCard = {
  vocabItemId?: string;
  hanzi?: string;
  pinyin?: string;
  glosses?: string[];
};
export type IPack = {
  id?: string;
  title?: string;
  hanzi?: string[];
};
export type IWordStrength = {
  hanzi?: string;
  pinyin?: string;
  glosses?: string[];
  strength?: number | string;
};
export type IDailyAccuracy = {
  date?: string;
  answered?: number | string;
  correct?: number | string;
};
export type IStatsReport = {
  poolSize?: number | string;
  totalWords?: number | string;
  words?: IWordStrength[];
  accuracy?: IDailyAccuracy[];
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
export const {
  useGetStatusQuery,
  useGetNextCardQuery,
  useGetLearnRoundQuery,
  useGetPacksQuery,
  useGetStatsQuery,
  useAnswerCardMutation,
} = injectedRtkApi;
