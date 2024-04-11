import {format} from 'date-fns';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useNavigate, useParams} from 'react-router-dom';

import {InformationCircleIcon, PencilIcon, TrashIcon} from '@heroicons/react/24/outline';
import {Badge, Text} from '@radix-ui/themes';
import {
  createColumnHelper, flexRender, getCoreRowModel, useReactTable,
} from '@tanstack/react-table';

import {BgtAvatar} from '../../../components/BgtAvatar/BgtAvatar';
import {BgtIconButton} from '../../../components/BgtIconButton/BgtIconButton';
import {BgtCard} from '../../../components/BgtLayout/BgtCard';
import {BgtNoData} from '../../../components/BgtNoData/BgtNoData';
import {useGame} from '../../../hooks/useGame';
import {useLocations} from '../../../hooks/useLocations';
import {usePlayers} from '../../../hooks/usePlayers';
import {useSettings} from '../../../hooks/useSettings';
import {Play} from '../../../models';
import {PlayFlagToString} from '../../../utils/playFlagToString';

const columnHelper = createColumnHelper<Play>()

const deletePlay = (id: number) => {
  console.log("delete: " + id)
}

const editPlay = (id: number) => {
  console.log("edit: " + id)
}

const DesktopDetails = () => {
  const { id } = useParams();
  const { plays } = useGame(id);
  const { t } = useTranslation();
  const { byId: playerById } = usePlayers();
  const { byId: locationById } = useLocations();
  const { settings } = useSettings();
  const navigate = useNavigate();
  const [data, _setData] = useState(() => [...plays])

  const columns = [
    columnHelper.accessor(row => row.start, {
      minSize: 200,
      id: 'start',
      header: () => <>{t('common.start', {})}</>,
      cell: info => (<div className='flex flex-col justify-center flex-none'>
        <div className='font-bold'>{format(info.getValue(), settings?.dateFormat ?? '')}</div>
        <div className='text-xs'>{format(info.getValue(), settings?.timeFormat ?? '')}</div>
      </div>),
    }),
    columnHelper.accessor(row => row.players.filter(x => x.won).sort(x => x.score ?? 0), {
      id: 'winners',
      header: () => <>{t('common.winners', {})}</>,
      cell: info => (<div className='flex gap-1 justify-center flex-wrap'>
        {info.getValue().map((player) => <BgtAvatar
          onClick={() => navigate(`/players/${player.playerId}`)}
          title={playerById(player.playerId)?.name}
          image={playerById(player.playerId)?.image}
          key={player.playerId}
        />)}
      </div>)
    }),
    columnHelper.accessor(row => row.players.filter(x => !x.won).sort(x => x.score ?? 0), {
      id: 'others',
      header: () => <>{t('common.loosers')}</>,
      cell: info => (<div className='flex gap-1 justify-center flex-wrap'>
        {info.getValue().map((player) => <BgtAvatar
          onClick={() => navigate(`/players/${player.playerId}`)}
          title={playerById(player.playerId)?.name}
          image={playerById(player.playerId)?.image}
          key={player.playerId}
        />)}
      </div>)
    }),
    columnHelper.accessor(row => row.minutes, {
      id: 'duration',
      header: () => <>{t('common.duration')}</>,
      //TODO: place minutes in base.json
      cell: info => <i>{info.getValue()} minutes</i>,
      footer: info => info.column.id,
    }),
    columnHelper.accessor(row => row.locationId, {
      id: 'location',
      header: () => <>{t('common.location')}</>,
      cell: info => <i>{locationById(info.getValue())?.name}</i>,
      footer: info => info.column.id,
    }),
    columnHelper.accessor(row => row.playFlags, {
      id: 'flags',
      header: () => <>{t('common.flags.header')}</>,
      cell: info => (
        <div className='flex flex-row justify-center gap-1 flex-wrap'>
          {
            info.getValue().map(flag => (
              <Badge key={flag} variant="solid">{t(PlayFlagToString(flag))}</Badge>
            ))
          }
        </div>),
      footer: info => info.column.id,
    }),
    columnHelper.accessor(row => row.id, {
      id: 'actions',
      size: 80,
      header: () => <>{t('common.actions')}</>,
      cell: info => (
        <div className='flex flex-row justify-center gap-1'>
          <BgtIconButton size={17} icon={<InformationCircleIcon />} onClick={() => editPlay(info.getValue())} />
          <BgtIconButton size={17} icon={<PencilIcon />} onClick={() => editPlay(info.getValue())} />
          <BgtIconButton size={17} icon={<TrashIcon />} onClick={() => deletePlay(info.getValue())} color='text-red-600' hoverColor='text-red-400' />
        </div>),
      footer: info => info.column.id,
    }),
  ]

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  if (settings === undefined) return null;

  if (plays.length === 0) {
    return (
      <BgtCard
        title={t('games.cards.games')}
        className='hidden md:flex md:flex-1'>
        <BgtNoData />
      </BgtCard>
    )
  }

  return (
    <BgtCard
      title={t('games.cards.games')}
      className='hidden md:flex md:flex-1'>
      <div className='rounded-md -mx-3 -mb-3'>
        <table className='table-auto w-full'>
          <thead>
            {table.getHeaderGroups().map(group => (
              <tr key={group.id}>
                {
                  group.headers.map(header => (
                    <th
                      className='pb-3'
                      key={header.id}
                      colSpan={header.colSpan}
                      style={{ width: header.getSize() }}
                    >
                      {header.isPlaceholder
                        ? null
                        : flexRender(header.column.columnDef.header, header.getContext())
                      }
                    </th>
                  ))
                }
              </tr>
            ))}
          </thead>
          <tbody>
            {table.getRowModel().rows.map(row => (
              <tr key={row.id} className='divide-x divide-sky-600 [&:nth-child(even)]:bg-sky-950'>
                {row.getVisibleCells().map(cell => (
                  <td key={cell.id} className='py-3 px-2 text-center'>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
        <div className='flex flex-row justify-end m-3'>
          pagination placeholder
        </div>
      </div>
    </BgtCard>
  )
}

const MobileDetails = () => {
  const { id } = useParams();
  const { plays } = useGame(id);
  const { t } = useTranslation();
  const { settings } = useSettings();
  const navigate = useNavigate();
  const { byId: playerById } = usePlayers();
  const { byId: locationById } = useLocations();

  if (settings === undefined) return null;

  if (plays.length === 0) {
    return (
      <BgtCard
        title={t('games.cards.games')}
        contentStyle='bg-sky-800'
        className='md:hidden'>
        <BgtNoData />
      </BgtCard>
    )
  }

  return (
    <div className='md:hidden rounded-md flex flex-col gap-3'>
      {
        plays.map((play, i) => <BgtCard
          title={i === 0 ? t('games.cards.games') : undefined}
          contentStyle='bg-sky-800'
          key={play.id}>
          <div className='flex flex-col gap-3'>
            <div className='flex flex-row justify-between'>
              <div className='flex flex-col justify-center flex-none'>
                <div className='font-bold'>{format(play.start, settings?.dateFormat ?? '')}</div>
                <div className='text-xs'>{format(play.start, settings?.timeFormat ?? '')}</div>
              </div>
              <div className='flex flex-row justify-center gap-1'>
                <BgtIconButton size={17} icon={<InformationCircleIcon />} onClick={() => editPlay(play.id)} />
                <BgtIconButton size={17} icon={<PencilIcon />} onClick={() => editPlay(play.id)} />
                <BgtIconButton size={17} icon={<TrashIcon />} onClick={() => deletePlay(play.id)} color='text-red-600' hoverColor='text-red-400' />
              </div>
            </div>
            <div className='flex flex-row justify-center gap-1'>
              {
                play.playFlags.map(flag => (
                  <Badge key={flag} variant="solid">{t(PlayFlagToString(flag))}</Badge>
                ))
              }
            </div>
            <div className='flex flex-row justify-between'>
              <div className='flex flex-col'>
                <Text weight="bold" align="left">{t('common.winners', {})}</Text>
                <div className='flex gap-1 justify-start'>
                  {play.players.filter(x => x.won).map((player) => <BgtAvatar
                    onClick={() => navigate(`/players/${player.playerId}`)}
                    title={playerById(player.playerId)?.name}
                    image={playerById(player.playerId)?.image}
                    key={player.playerId}
                  />
                  )}
                </div>
              </div>
              <div className='flex flex-col'>
                <Text weight="bold" align="right">{t('common.loosers')}</Text>
                <div className='flex gap-1 justify-end'>
                  {play.players.filter(x => !x.won).map((player) => <BgtAvatar
                    onClick={() => navigate(`/players/${player.playerId}`)}
                    title={playerById(player.playerId)?.name}
                    image={playerById(player.playerId)?.image}
                    key={player.playerId}
                  />
                  )}
                </div>
              </div>
            </div>
            <div className='flex flex-row justify-between'>
              <div className='flex flex-col'>
                <Text weight="bold" align="left">{t('common.duration')}</Text>
                <div className='flex gap-1 justify-start'>
                  {play.minutes} minutes
                </div>
              </div>
              <div className='flex flex-col justify-end'>
                <Text weight="bold" align="right">{t('common.location')}</Text>
                <div className='flex gap-1 justify-end'>
                  {locationById(play.locationId)?.name}
                </div>
              </div>
            </div>
          </div>
        </BgtCard>)
      }
    </div>
  )
}

export const GamePlays = () => {
  return (
    <>
      <DesktopDetails />
      <MobileDetails />
    </>
  )
}
