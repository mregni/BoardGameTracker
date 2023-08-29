import {Avatar, Popconfirm, Space, Table, Tooltip} from 'antd';
import {ColumnsType} from 'antd/es/table';
import {format, formatDuration, minutesToHours} from 'date-fns';
import {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {DeleteOutlined, EditOutlined} from '@ant-design/icons';

import GcBooleanIcon from '../../../components/GcBooleanIcon/GcBooleanIcon';
import {SettingsContext} from '../../../context/settingsContext';
import {Play, PlayPlayer} from '../../../models';
import {PlayerContext} from '../../Players/context';
import {EditPlayDrawer} from '../../Plays';
import {GameDetailContext} from '../context/GameDetailState';

export const GameContent = () => {
  const { plays, deletePlay } = useContext(GameDetailContext);
  const { settings } = useContext(SettingsContext);
  const { players } = useContext(PlayerContext);
  const [openPlayEdit, setOpenPlayEdit] = useState(false);
  const [playToEdit, setPlayToEdit] = useState<Play | null>(null);
  const { t } = useTranslation();

  const editPlay = (id: number): void => {
    setPlayToEdit(plays.filter(play => play.id === id)[0]);
    setOpenPlayEdit(true);
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
      title: 'Players',
      key: 'players',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => {
        return (
          <Avatar.Group>
            {
              data.map((player) => {
                const index = players.findIndex(p => p.id === player.playerId);
                if (index !== undefined) {
                  return (
                    <Tooltip title={players[index].name} placement="top" key={player.playerId}>
                      <Avatar style={{ backgroundColor: '#87d068' }} src={`https://localhost:7178/${players[index].image}`} />
                    </Tooltip>
                  )
                }
                return "";
              })
            }
          </Avatar.Group>
        )
      }
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
      title: 'Winners',
      key: 'won',
      dataIndex: 'players',
      render: (data: PlayPlayer[]) => {
        return (
          <Avatar.Group>
            {
              data.filter(x => x.won).map((player) => {
                const index = players.findIndex(p => p.id === player.playerId);
                if (index !== undefined) {
                  return (
                    <Tooltip title={players[index].name} placement="top" key={player.playerId}>
                      <Avatar style={{ backgroundColor: '#87d068' }} src={`https://localhost:7178/${players[index].image}`} />
                    </Tooltip>
                  )
                }
                return "";
              })
            }
          </Avatar.Group>
        )
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
    }, {
      title: 'Actions',
      key: 'actions',
      align: 'right',
      width: 70,
      render: (data: Play) =>
        <Space>
          <Tooltip title="Edit" placement="topRight">
            <EditOutlined onClick={() => editPlay(data.id)} />
          </Tooltip>
          <Popconfirm
            placement="left"
            title={t('play.delete.title')}
            description={t('play.delete.description')}
            onConfirm={() => deletePlay(data.id)}
            okText={t('common.yes')}
            cancelText={t('common.no')}
          >
            <DeleteOutlined style={{ color: '#a8071a' }} />
          </Popconfirm>
        </Space >
    }
  ];

  return (
    <Space direction='vertical' style={{ display: 'flex' }}>
      <Table columns={columns} dataSource={plays} size="small" rowKey={(play: Play) => play.id} />
      {playToEdit && <EditPlayDrawer open={openPlayEdit} setOpen={setOpenPlayEdit} play={playToEdit as Play} />}
    </Space>
  )
}