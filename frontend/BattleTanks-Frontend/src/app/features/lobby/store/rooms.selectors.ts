import { createFeatureSelector, createSelector } from '@ngrx/store';
import { roomsAdapter, RoomsState } from './rooms.reducer';

export const selectRoomsState = createFeatureSelector<RoomsState>('rooms');

const { selectAll } = roomsAdapter.getSelectors();

export const selectRooms = createSelector(selectRoomsState, (s) => selectAll(s));
export const selectRoomsLoading = createSelector(selectRoomsState, (s) => s.loading);
export const selectRoomsCreating = createSelector(selectRoomsState, (s) => s.creating);
export const selectRoomsError = createSelector(selectRoomsState, (s) => s.error);
