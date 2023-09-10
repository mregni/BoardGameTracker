import {Avatar, Tooltip} from 'antd';
import React, {useContext} from 'react';
import {Link} from 'react-router-dom';

import {PlayPlayer} from '../../models';
import {PlayerContext} from '../../pages/Players/context';

type Props = {
  playData: PlayPlayer[];
}

export const GcAvatarGroup = (props: Props) => {
  const { playData } = props;
  const { players } = useContext(PlayerContext);

  return (
    <Avatar.Group>
      {
        playData.map((player) => {
          const index = players.findIndex(p => p.id === player.playerId);
          if (index !== undefined) {
            return (
              <Tooltip title={players[index].name} placement="top" key={player.playerId}>
                <Link to={`/players/${players[index].id}`}>
                  <Avatar style={{ backgroundColor: '#87d068' }} src={`https://localhost:7178/${players[index].image}`} />
                </Link>
              </Tooltip>
            )
          }
          return "";
        })
      }
    </Avatar.Group>
  )
}
