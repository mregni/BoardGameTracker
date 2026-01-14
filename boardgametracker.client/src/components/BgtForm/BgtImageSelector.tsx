import { useTranslation } from 'react-i18next';
import { ChangeEvent, Dispatch, DragEvent, SetStateAction, useState } from 'react';
import { cx } from 'class-variance-authority';

import { BgtIconButton } from '../BgtIconButton/BgtIconButton';

import TrashIcon from '@/assets/icons/trash.svg?react';

interface Props {
  image: File | undefined | null;
  setImage: Dispatch<SetStateAction<File | undefined | null>>;
  defaultImage?: string | null;
}

interface ImagePreviewProps {
  src: string;
  onRemove: () => void;
}

const ImagePreview = (props: ImagePreviewProps) => {
  const { src, onRemove } = props;

  return (
    <div className="group relative">
      <img alt="preview image" src={src} className="w-28 h-28 rounded-lg" />
      <div className="absolute top-0 left-0 w-full h-full collapse group-hover:visible">
        <div className="flex justify-center items-center w-full h-full">
          <BgtIconButton
            size="big"
            className="rounded-full! border-solid border bg-[rgba(0,0,0,0.4)] hover:bg-[rgba(0,0,0,0.6)]"
            icon={<TrashIcon className="size-5" color="white" />}
            onClick={onRemove}
          />
        </div>
      </div>
    </div>
  );
};

export const BgtImageSelector = (props: Props) => {
  const { image, setImage, defaultImage } = props;
  const { t } = useTranslation();
  const [isDragging, setIsDragging] = useState(false);
  const [hasDefaultImage, setHasDefaultImage] = useState(!!defaultImage);

  const onImageChangeViaInput = (event: ChangeEvent<HTMLInputElement>) => {
    if (event.target.files?.[0]) {
      setImage(event.target.files[0]);
    }
  };

  const onImageChangeViaDrop = (event: DragEvent<HTMLLabelElement>) => {
    event.stopPropagation();
    event.preventDefault();
    setIsDragging(false);

    if (event.dataTransfer.files?.[0]) {
      setImage(event.dataTransfer.files[0]);
    }
  };

  const onDragOver = (event: DragEvent<HTMLLabelElement>) => {
    event.stopPropagation();
    event.preventDefault();
  };

  const onDragEnter = (event: DragEvent<HTMLLabelElement>) => {
    event.stopPropagation();
    event.preventDefault();
    setIsDragging(true);
  };

  const onDragLeave = (event: DragEvent<HTMLLabelElement>) => {
    event.stopPropagation();
    event.preventDefault();
    setIsDragging(false);
  };

  const handleRemoveDefaultImage = () => {
    setImage(null);
    setHasDefaultImage(false);
  };

  const handleRemoveNewImage = () => {
    setImage(undefined);
    setHasDefaultImage(false);
  };

  return (
    <div className="flex justify-start w-full flex-col">
      <div className="flex flex-row justify-start gap-3">
        {hasDefaultImage && !image && defaultImage && (
          <ImagePreview src={defaultImage} onRemove={handleRemoveDefaultImage} />
        )}
        {image && <ImagePreview src={URL.createObjectURL(image)} onRemove={handleRemoveNewImage} />}
        {!image && !hasDefaultImage && (
          <div
            className={cx(
              'group border-gray-500 hover:border-gray-400 border-dashed border-2 rounded-md aspect-square h-28 p-2 cursor-pointer',
              isDragging && 'border-gray-400'
            )}
          >
            <label
              htmlFor="dropzone-file"
              onDrop={onImageChangeViaDrop}
              onDragOver={onDragOver}
              onDragEnter={onDragEnter}
              onDragLeave={onDragLeave}
              className={cx(
                'flex flex-col items-center justify-center group-hover:bg-gray-800 rounded-md cursor-pointer h-full',
                isDragging && 'bg-gray-800'
              )}
            >
              <div className="flex flex-col items-center justify-center">
                <p className="mb-2 text-sm text-gray-500 text-center">
                  <span className="font-semibold">{t('images.upload')}</span>
                </p>
                <p className="text-xs text-gray-500">{t('images.types')}</p>
              </div>
              <input id="dropzone-file" onChange={onImageChangeViaInput} type="file" className="hidden" />
            </label>
          </div>
        )}
      </div>
    </div>
  );
};
