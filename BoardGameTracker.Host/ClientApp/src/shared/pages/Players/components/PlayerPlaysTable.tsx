import {Space, Table} from 'antd';
import {ColumnsType} from 'antd/es/table';
import {format, formatDuration, minutesToHours} from 'date-fns';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {Link} from 'react-router-dom';

import {GcAvatar} from '../../../components/GcAvatar';
import {GcAvatarGroup} from '../../../components/GcAvatarGroup/GcAvatarGroup';
import GcBooleanIcon from '../../../components/GcBooleanIcon/GcBooleanIcon';
import {GcActionButtons} from '../../../components/GcTable/GcActionButtons';
import {SettingsContext} from '../../../context/settingsContext';
import {usePagination} from '../../../hooks';
import {Play, PlayPlayer} from '../../../models';
import {limitStringLength} from '../../../utils';
import {GamesContext} from '../../Games/context';
import {EditPlayDrawer} from '../../Plays';
import {PlayerDetailContext} from '../context/PlayerDetailState';

export const PlayerPlaysTable = () => {
  const { plays, deletePlayerPlay, updatePlayerPlay } = useContext(PlayerDetailContext);
  const { settings } = useContext(SettingsContext);
  const { games } = useContext(GamesContext);
  const [openPlayEdit, setOpenPlayEdit] = useState(false);
  const [playToEdit, setPlayToEdit] = useState<Play | null>(null);
  const { getPagination } = usePagination();
  const { t } = useTranslation();

  const editPlay = (id: number): void => {
    setPlayToEdit(plays.filter(play => play.id === id)[0]);
    setOpenPlayEdit(true);
  }

  const columns: ColumnsType<Play> = [
    {
      title: t('common.date'),
      key: 'date',
      render: (data: Play) => {
        return format(data.start, settings.dateTimeFormat);
      }
    },
    {
      title: t('common.game'),
      dataIndex: 'gameId',
      render: (id: number) => {
        return (
          <GcAvatar
            image={games.filter((game) => game.id === id)[0]?.image}
            link={games.filter((game) => game.id === id)[0]?.id}
            label={games.filter((game) => game.id === id)[0]?.title}
          />
        )
      }
    },
    {
      title: t('common.players'),
      key: 'players',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => <GcAvatarGroup playData={data} />
    },
    {
      title: t('common.won'),
      key: 'won',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => <GcAvatarGroup playData={data.filter(x => x.won)} />
    },
    {
      title: t('common.time-played'),
      key: 'length',
      render: (data: Play) => {
        const hours = minutesToHours(data.minutes);
        const minutes = data.minutes - (hours * 60);
        return formatDuration({ hours: minutesToHours(data.minutes), minutes: minutes });
      }
    },
    {
      title: t('common.ended'),
      key: 'ended',
      dataIndex: 'ended',
      width: 80,
      render: (x: boolean) => <GcBooleanIcon value={x} />
    },
    {
      title: t('common.comment'),
      key: 'comment',
      dataIndex: 'comment',
      render: ((comment: string) => limitStringLength(comment))
    },
    {
      title: t('common.actions'),
      key: 'actions',
      align: 'right',
      width: 70,
      render: (data: Play) => <GcActionButtons
        id={data.id}
        title={t('play.delete.title')}
        description={t('play.delete.description')}
        edit={editPlay}
        remove={deletePlayerPlay}
      />
    }
  ];


  return (
    <Space direction='vertical' style={{ display: 'flex' }}>
      <Table
        columns={columns}
        dataSource={plays}
        size="small"
        rowKey={(play: Play) => play.id}
        pagination={getPagination(plays.length)}
      />
      {
        playToEdit && <EditPlayDrawer
          open={openPlayEdit}
          setOpen={setOpenPlayEdit}
          play={playToEdit as Play}
          edit={updatePlayerPlay}
        />
      }
    </Space>
  )
}
