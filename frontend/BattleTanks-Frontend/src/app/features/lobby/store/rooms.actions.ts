import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { CreateRoomDto, RoomStateDto } from '../../../core/models/room.models';

export const roomsActions = createActionGroup({
  source: 'Rooms',
  events: {
    'Load Rooms': emptyProps(),                                    
    'Load Rooms Success': props<{ rooms: RoomStateDto[] }>(),       
    'Load Rooms Failure': props<{ error: string }>(),

    'Create Room': props<{ dto: CreateRoomDto }>(),
    'Create Room Success': props<{ room: RoomStateDto }>(),
    'Create Room Failure': props<{ error: string }>(),
  },
});
