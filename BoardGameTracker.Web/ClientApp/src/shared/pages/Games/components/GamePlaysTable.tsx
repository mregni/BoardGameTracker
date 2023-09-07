import {Space, Table} from 'antd';
import {ColumnsType, TablePaginationConfig} from 'antd/es/table';
import {format, formatDuration, minutesToHours} from 'date-fns';
import {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {GcAvatarGroup} from '../../../components/GcAvatarGroup/GcAvatarGroup';
import GcBooleanIcon from '../../../components/GcBooleanIcon/GcBooleanIcon';
import {GcActionButtons} from '../../../components/GcTable/GcActionButtons';
import {SettingsContext} from '../../../context/settingsContext';
import {usePagination} from '../../../hooks';
import {Play, PlayPlayer} from '../../../models';
import {limitStringLength} from '../../../utils';
import {EditPlayDrawer} from '../../Plays';
import {GameDetailContext} from '../context/GameDetailState';

export const GamePlaysTable = () => {
  const { plays, deleteGamePlay, updateGamePlay } = useContext(GameDetailContext);
  const { settings } = useContext(SettingsContext);
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
      render: (data: PlayPlayer[]) => <GcAvatarGroup playData={data} />
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
      render: (data: PlayPlayer[]) => <GcAvatarGroup playData={data.filter(x => x.won)} />
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
      render: (data: Play) => <GcActionButtons
        id={data.id}
        title={t('play.delete.title')}
        description={t('play.delete.description')}
        edit={editPlay}
        remove={deleteGamePlay}
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
          edit={updateGamePlay}
        />}
    </Space>
  )
}