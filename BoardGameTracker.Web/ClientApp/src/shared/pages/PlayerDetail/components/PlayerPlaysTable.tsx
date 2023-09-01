import {Space, Table, TablePaginationConfig} from 'antd';
import {ColumnsType} from 'antd/es/table';
import {format, formatDuration, minutesToHours} from 'date-fns';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {Link} from 'react-router-dom';

import {GcAvatarGroup} from '../../../components/GcAvatarGroup/GcAvatarGroup';
import GcBooleanIcon from '../../../components/GcBooleanIcon/GcBooleanIcon';
import {GcActionButtons} from '../../../components/GcTable/GcActionButtons';
import {SettingsContext} from '../../../context/settingsContext';
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
  const { t } = useTranslation();

  const editPlay = (id: number): void => {
    setPlayToEdit(plays.filter(play => play.id === id)[0]);
    setOpenPlayEdit(true);
  }

  const pagination: TablePaginationConfig = {
    position: ['bottomRight'],
    total: plays.length,
    defaultCurrent: 1,
    hideOnSinglePage: false,
    showSizeChanger: true,
    showTitle: true
  }

  const columns: ColumnsType<Play> = [
    {
      title: 'Date',
      key: 'date',
      render: (data: Play) => {
        return format(data.start, settings.dateTimeFormat);
      }
    },
    {
      title: 'Game',
      dataIndex: 'gameId',
      render: (id: number) => {
        return (
        <Link to={`/games/${id}`}>
          {games.filter((game) => game.id === id)[0]?.title}
        </Link>)
      }
    },
    {
      title: 'Players',
      key: 'players',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => <GcAvatarGroup playData={data} />
    },
    {
      title: 'Won',
      key: 'won',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => <GcAvatarGroup playData={data.filter(x => x.won)} />
    },
    {
      title: 'Time played',
      key: 'length',
      render: (data: Play) => {
        const hours = minutesToHours(data.minutes);
        const minutes = data.minutes - (hours * 60);
        return formatDuration({ hours: minutesToHours(data.minutes), minutes: minutes });
      }
    },
    {
      title: 'Ended',
      key: 'ended',
      dataIndex: 'ended',
      width: 80,
      render: (x: boolean) => <GcBooleanIcon value={x} />
    },
    {
      title: 'Comment',
      key: 'comment',
      dataIndex: 'comment',
      render: ((comment: string) => limitStringLength(comment))
    },
    {
      title: 'Actions',
      key: 'actions',
      align: 'right',
      width: 70,
      render: (data: Play) => <GcActionButtons play={data} editPlay={editPlay} deletePlay={deletePlayerPlay} />
    }
  ];


  return (
    <Space direction='vertical' style={{ display: 'flex' }}>
      <Table columns={columns} dataSource={plays} size="small" rowKey={(play: Play) => play.id} pagination={pagination} />
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
