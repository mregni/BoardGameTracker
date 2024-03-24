import clsx from 'clsx';
import {ChangeEvent, DragEvent, useState} from 'react';
import {useForm} from 'react-hook-form';
import {useTranslation} from 'react-i18next';
import * as z from 'zod';

import {ArrowUpTrayIcon, PhotoIcon, TrashIcon} from '@heroicons/react/24/outline';
import {zodResolver} from '@hookform/resolvers/zod';
import * as Form from '@radix-ui/react-form';
import {Button, Dialog} from '@radix-ui/themes';

import {useImages} from '../../hooks/useImages';
import {usePlayers} from '../../hooks/usePlayers';
import {Player} from '../../models';
import {BgtInputField} from '../BgtForm/BgtInputField';
import {BgtIcon} from '../BgtIcon/BgtIcon';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

export const BgtPlayerModal = (props: Props) => {
  const { open, setOpen } = props;
  const [isDragging, setIsDragging] = useState(false);
  const { t } = useTranslation();
  const [image, setImage] = useState<File | undefined>(undefined);

  const { isPending, uploadPlayerImage } = useImages();
  const { save } = usePlayers();

  const onImageChangeViaInput = (event: ChangeEvent<HTMLInputElement>) => {
    if (event.target.files?.[0]) {
      setImage(event.target.files[0]);
    }

    return false;
  }

  const onImageChangeViaDrop = (event: DragEvent<HTMLLabelElement>) => {
    setIsDragging(false);
    event.stopPropagation();
    event.preventDefault();

    if (event.dataTransfer.files?.[0]) {
      setImage(event.dataTransfer.files[0]);
    }

    return false;
  }

  const onDragOver = (event: DragEvent<HTMLLabelElement>) => {
    event.stopPropagation();
    event.preventDefault();

    return false;
  }

  const onDragEnter = (event: DragEvent<HTMLLabelElement>) => {
    event.stopPropagation();
    event.preventDefault();
    setIsDragging(true);
  }

  const onDragLeave = (event: DragEvent<HTMLLabelElement>) => {
    event.stopPropagation();
    event.preventDefault();
    setIsDragging(false);
  }

  interface FormProps {
    name: string
  }

  const schema = z.object({
    name: z.string().min(1, { message: t('player.new.name.required') })
  });

  const { register, handleSubmit, formState: { errors } } = useForm<FormProps>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: FormProps) => {
    const savedImage = await uploadPlayerImage(image);
    console.log(savedImage);

    const player: Player = {
      id: 0,
      name: data.name,
      image: savedImage?.model ?? ''
    };

    await save(player);
    setOpen(false);
  };

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>
          {t('player.new.title')}
        </Dialog.Title>
        <Dialog.Description>
          {t('player.new.description')}
        </Dialog.Description>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className='flex flex-col gap-3 mt-3 mb-6' >
            <BgtInputField
              type='text'
              placeholder={t('player.new.name.placeholder')}
              name='name'
              label={t('common.name')}
              register={register}
              errors={errors}
            />
            <div className="flex justify-start w-full flex-col h-44">
              <div className="text-[15px] font-medium leading-[35px]">
                {t('common.profile-picture')}
              </div>
              <div className='flex flex-row justify-start gap-3'>
                {image && (
                  <div>
                    <img alt="preview image" src={URL.createObjectURL(image)} className='w-28 h-28 rounded-md mb-1' />
                    <Button color='red' onClick={() => setImage(undefined)} className='w-28'>
                      <BgtIcon icon={<TrashIcon />} height={18} width={18} />
                    </Button>
                  </div>
                )}
                {!image && (
                  <>
                    <div className="flex flex-col items-center justify-center w-28 h-28 rounded-md shadow-2xl border-gray-500 border-2 border-dashed">
                      <div className="flex flex-col items-center justify-center" >
                        <BgtIcon
                          icon={<PhotoIcon />}
                          className='text-gray-500'
                        />
                      </div>
                    </div>
                  </>
                )}
                <label
                  htmlFor="dropzone-file"
                  onDrop={onImageChangeViaDrop}
                  onDragOver={onDragOver}
                  onDragEnter={onDragEnter}
                  onDragLeave={onDragLeave}
                  className={clsx(
                    "flex flex-col items-center justify-center grow h-28 border-2 border-gray-500 hover:border-gray-400 hover:bg-gray-800 border-dashed rounded-md cursor-pointer",
                    isDragging && "border-gray-400 bg-gray-800")}>
                  <div className="flex flex-col items-center justify-center" >
                    <BgtIcon
                      icon={<ArrowUpTrayIcon />}
                      className='text-gray-500'
                    />
                    <p className="mb-2 text-sm text-gray-500"><span className="font-semibold">{t('images.upload')}</span><span className='hidden sm:inline'>{t('images.drag-and-drop')}</span></p>
                    <p className="text-xs text-gray-500">{t('images.types')}</p>
                  </div>
                  <input id="dropzone-file" onChange={onImageChangeViaInput} type="file" className="hidden" />
                </label>
              </div>
            </div>
          </div>

          <div className='flex justify-end gap-3'>
            <Dialog.Close>
              <>
                <Form.Submit asChild>
                  <Button type='submit' variant='surface' color='orange' disabled={isPending}>
                    {t('player.new.save')}
                  </Button>
                </Form.Submit>
                <Button variant='surface' color='gray' onClick={() => setOpen(false)}>
                  {t('common.cancel')}
                </Button>
              </>
            </Dialog.Close>
          </div>
        </form>
      </Dialog.Content>
    </Dialog.Root >
  )
}