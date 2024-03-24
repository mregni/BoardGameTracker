import {Player} from '../models';

export const getPlayerImage = (playerId: number, players: Player[] | undefined): string => {
  if (players === undefined) {
    return '';
  }

  const index = players.findIndex(x => x.id === playerId);
  if (index !== -1) {
    return players[index].image;
  }

  return '';
}

export const getPlayerName = (playerId: number, players: Player[] | undefined): string => {
  if (players === undefined) {
    return '';
  }

  const index = players.findIndex(x => x.id === playerId);
  if (index !== -1) {
    return players[index].name;
  }

  return '';
}