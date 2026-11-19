import { isAvatarId, type AvatarId } from './playerAvatars'

export const RoomPhase = {
  WaitingForPlayers: 0,
  StorytellerChoosing: 1,
  GuessersSubmitting: 2,
  Voting: 3,
  Reveal: 4,
  Scoring: 5,
  RoundComplete: 6,
  GameComplete: 7,
  VoteReveal: 8,
} as const

export type RoomPhase = (typeof RoomPhase)[keyof typeof RoomPhase]

export interface PlayerStateResponse {
  playerId: string
  name: string
  avatarId: AvatarId
  isConnected: boolean
}

export interface VotingCardVoteStateResponse {
  voterPlayerId: string
  voterName: string
}

export interface VotingCardStateResponse {
  votingCardId: string
  cardId: string
  displayNumber: number
  votes: VotingCardVoteStateResponse[]
}

export interface RevealedVoteStateResponse {
  voterPlayerId: string
  voterName: string
}

export interface RevealCardStateResponse {
  votingCardId: string
  cardId: string
  displayNumber: number
  ownerPlayerId: string
  ownerName: string
  isStorytellerCard: boolean
  votes: RevealedVoteStateResponse[]
}

export interface RoundScoreStateResponse {
  playerId: string
  playerName: string
  pointsAwarded: number
}

export interface ScoreboardEntryStateResponse {
  playerId: string
  playerName: string
  totalPoints: number
}

export interface WinningPlayerStateResponse {
  playerId: string
  playerName: string
  totalPoints: number
}

export interface RoomStateResponse {
  roomCode: string
  phase: RoomPhase
  targetScore: number
  players: PlayerStateResponse[]
  currentStoryteller: PlayerStateResponse | null
  clue: string | null
  submissionCount: number
  expectedSubmissionCount: number
  voteCount: number
  expectedVoteCount: number
  votingCards: VotingCardStateResponse[]
  revealedCards: RevealCardStateResponse[]
  roundScores: RoundScoreStateResponse[]
  scoreboard: ScoreboardEntryStateResponse[]
  winners: WinningPlayerStateResponse[]
  minimumPlayers: number
  maximumPlayers: number
  hostPlayerId: string | null
}

export interface RoomUpdatedEvent {
  room: RoomStateResponse
}

export interface PrivateCardStateResponse {
  cardId: string
}

export interface PlayerPrivateStateResponse {
  playerId: string
  name: string
  hand: PrivateCardStateResponse[]
  isCurrentStoryteller: boolean
  hasSubmittedGuessCard: boolean
  submittedVotingCardId: string | null
  hasVoted: boolean
}

export interface GetPlayerPrivateStateResponse {
  room: RoomStateResponse
  player: PlayerPrivateStateResponse
}

export interface PlayerStateUpdatedEvent {
  state: GetPlayerPrivateStateResponse
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isString(value: unknown): value is string {
  return typeof value === 'string'
}

function isInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isInteger(value)
}

function isNullableString(value: unknown): value is string | null {
  return value === null || isString(value)
}

function isPlayerStateResponse(value: unknown): value is PlayerStateResponse {
  return isRecord(value)
    && isString(value.playerId)
    && isString(value.name)
    && isAvatarId(value.avatarId)
    && typeof value.isConnected === 'boolean'
}

function isPrivateCardStateResponse(
  value: unknown,
): value is PrivateCardStateResponse {
  return isRecord(value) && isString(value.cardId)
}

function isPlayerPrivateStateResponse(
  value: unknown,
): value is PlayerPrivateStateResponse {
  return isRecord(value)
    && isString(value.playerId)
    && isString(value.name)
    && Array.isArray(value.hand)
    && value.hand.every(isPrivateCardStateResponse)
    && typeof value.isCurrentStoryteller === 'boolean'
    && typeof value.hasSubmittedGuessCard === 'boolean'
    && isNullableString(value.submittedVotingCardId)
    && typeof value.hasVoted === 'boolean'
}

function isVotingCardVoteStateResponse(
  value: unknown,
): value is VotingCardVoteStateResponse {
  return isRecord(value)
    && isString(value.voterPlayerId)
    && isString(value.voterName)
}

function isVotingCardStateResponse(value: unknown): value is VotingCardStateResponse {
  return isRecord(value)
    && isString(value.votingCardId)
    && isString(value.cardId)
    && isInteger(value.displayNumber)
    && Array.isArray(value.votes)
    && value.votes.every(isVotingCardVoteStateResponse)
}

function isRevealedVoteStateResponse(
  value: unknown,
): value is RevealedVoteStateResponse {
  return isRecord(value)
    && isString(value.voterPlayerId)
    && isString(value.voterName)
}

function isRevealCardStateResponse(value: unknown): value is RevealCardStateResponse {
  return isRecord(value)
    && isString(value.votingCardId)
    && isString(value.cardId)
    && isInteger(value.displayNumber)
    && isString(value.ownerPlayerId)
    && isString(value.ownerName)
    && typeof value.isStorytellerCard === 'boolean'
    && Array.isArray(value.votes)
    && value.votes.every(isRevealedVoteStateResponse)
}

function isRoundScoreStateResponse(value: unknown): value is RoundScoreStateResponse {
  return isRecord(value)
    && isString(value.playerId)
    && isString(value.playerName)
    && isInteger(value.pointsAwarded)
}

function isScoreboardEntryStateResponse(
  value: unknown,
): value is ScoreboardEntryStateResponse {
  return isRecord(value)
    && isString(value.playerId)
    && isString(value.playerName)
    && isInteger(value.totalPoints)
}

function isWinningPlayerStateResponse(
  value: unknown,
): value is WinningPlayerStateResponse {
  return isRecord(value)
    && isString(value.playerId)
    && isString(value.playerName)
    && isInteger(value.totalPoints)
}

export function isRoomStateResponse(value: unknown): value is RoomStateResponse {
  return isRecord(value)
    && isString(value.roomCode)
    && isInteger(value.phase)
    && value.phase >= RoomPhase.WaitingForPlayers
    && value.phase <= RoomPhase.VoteReveal
    && isInteger(value.targetScore)
    && Array.isArray(value.players)
    && value.players.every(isPlayerStateResponse)
    && (value.currentStoryteller === null || isPlayerStateResponse(value.currentStoryteller))
    && isNullableString(value.clue)
    && isInteger(value.submissionCount)
    && isInteger(value.expectedSubmissionCount)
    && isInteger(value.voteCount)
    && isInteger(value.expectedVoteCount)
    && Array.isArray(value.votingCards)
    && value.votingCards.every(isVotingCardStateResponse)
    && Array.isArray(value.revealedCards)
    && value.revealedCards.every(isRevealCardStateResponse)
    && Array.isArray(value.roundScores)
    && value.roundScores.every(isRoundScoreStateResponse)
    && Array.isArray(value.scoreboard)
    && value.scoreboard.every(isScoreboardEntryStateResponse)
    && Array.isArray(value.winners)
    && value.winners.every(isWinningPlayerStateResponse)
    && isInteger(value.minimumPlayers)
    && isInteger(value.maximumPlayers)
    && isNullableString(value.hostPlayerId)
}

export function isRoomUpdatedEvent(value: unknown): value is RoomUpdatedEvent {
  return isRecord(value) && isRoomStateResponse(value.room)
}

export function isGetPlayerPrivateStateResponse(
  value: unknown,
): value is GetPlayerPrivateStateResponse {
  return isRecord(value)
    && isRoomStateResponse(value.room)
    && isPlayerPrivateStateResponse(value.player)
}

export function isPlayerStateUpdatedEvent(
  value: unknown,
): value is PlayerStateUpdatedEvent {
  return isRecord(value) && isGetPlayerPrivateStateResponse(value.state)
}
