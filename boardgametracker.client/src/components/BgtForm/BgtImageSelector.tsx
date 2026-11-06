import { cx } from "class-variance-authority";
import {
	type ChangeEvent,
	type Dispatch,
	type DragEvent,
	type SetStateAction,
	useEffect,
	useMemo,
	useState,
} from "react";
import { useTranslation } from "react-i18next";
import TrashIcon from "@/assets/icons/trash.svg?react";
import { BgtIconButton } from "../BgtIconButton/BgtIconButton";
import { BgtFallbackImage } from "../BgtImage/BgtFallbackImage";

interface Props {
	image: File | undefined | null;
	setImage: Dispatch<SetStateAction<File | undefined | null>>;
	defaultImage?: string | null;
	title?: string;
}

interface ImagePreviewProps {
	src: string;
	title?: string;
	onRemove: () => void;
}

const ImagePreview = (props: ImagePreviewProps) => {
	const { src, title, onRemove } = props;
	const { t } = useTranslation("images");

	return (
		<div className="relative w-28 h-28 rounded-lg overflow-hidden">
			<BgtFallbackImage title={title ?? t("selected-image")} image={src} />
			<div className="absolute top-1 right-1">
				<BgtIconButton
					size="2"
					className="rounded-full! border-solid border bg-[rgba(0,0,0,0.4)] hover:bg-[rgba(0,0,0,0.6)]"
					icon={<TrashIcon className="size-5" color="white" />}
					onClick={onRemove}
					aria-label={t("remove")}
				/>
			</div>
		</div>
	);
};

export const BgtImageSelector = (props: Props) => {
	const { image, setImage, defaultImage, title } = props;
	const { t } = useTranslation("images");
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

	const imagePreviewUrl = useMemo(() => (image ? URL.createObjectURL(image) : undefined), [image]);
	useEffect(() => {
		return () => {
			if (imagePreviewUrl) {
				URL.revokeObjectURL(imagePreviewUrl);
			}
		};
	}, [imagePreviewUrl]);

	return (
		<div className="flex justify-start w-full flex-col">
			<div className="flex flex-row justify-start gap-3">
				{hasDefaultImage && !image && defaultImage && (
					<ImagePreview src={defaultImage} title={title} onRemove={handleRemoveDefaultImage} />
				)}
				{imagePreviewUrl && <ImagePreview src={imagePreviewUrl} title={title} onRemove={handleRemoveNewImage} />}
				{!image && !hasDefaultImage && (
					<div
						className={cx(
							"group border-gray-500 hover:border-gray-400 border-dashed border-2 rounded-md aspect-square h-28 p-2 cursor-pointer",
							isDragging && "border-gray-400",
						)}
					>
						<label
							htmlFor="dropzone-file"
							onDrop={onImageChangeViaDrop}
							onDragOver={onDragOver}
							onDragEnter={onDragEnter}
							onDragLeave={onDragLeave}
							className={cx(
								"flex flex-col items-center justify-center group-hover:bg-gray-800 rounded-md cursor-pointer h-full",
								isDragging && "bg-gray-800",
							)}
						>
							<div className="flex flex-col items-center justify-center">
								<p className="mb-2 text-sm text-gray-400 text-center">
									<span className="font-semibold">{t("upload")}</span>
								</p>
								<p className="text-xs text-gray-400">{t("types")}</p>
							</div>
							<input
								id="dropzone-file"
								onChange={onImageChangeViaInput}
								type="file"
								accept="image/*"
								className="hidden"
							/>
						</label>
					</div>
				)}
			</div>
		</div>
	);
};
