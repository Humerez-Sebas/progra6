import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { RoomStateDto } from '../../../core/models/room.models';
import { roomsActions } from './rooms.actions';

export interface RoomsState extends EntityState<RoomStateDto> {
  loading: boolean;
  creating: boolean;
  error: string | null;
}

export const roomsAdapter = createEntityAdapter<RoomStateDto>({
  selectId: (r) => r.roomId,
});

const initialState: RoomsState = roomsAdapter.getInitialState({
  loading: false,
  creating: false,
  error: null,
});

export const roomsReducer = createReducer(
  initialState,

  on(roomsActions.loadRooms, (state) => ({ ...state, loading: true, error: null })),

  on(roomsActions.loadRoomsSuccess, (state, { rooms }) =>
  roomsAdapter.setAll(Array.isArray(rooms) ? rooms : [], { ...state, loading: false })
),

  on(roomsActions.loadRoomsFailure, (state, { error }) => ({ ...state, loading: false, error })),

  on(roomsActions.createRoom, (state) => ({ ...state, creating: true, error: null })),
  on(roomsActions.createRoomSuccess, (state, { room }) =>
    roomsAdapter.upsertOne(room, { ...state, creating: false })
  ),
  on(roomsActions.createRoomFailure, (state, { error }) => ({ ...state, creating: false, error })),
);
