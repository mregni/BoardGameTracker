import * as Dialog from "@radix-ui/react-dialog";
import { cx } from "class-variance-authority";
import { type ComponentPropsWithoutRef, createContext, type ReactNode, useContext } from "react";
import { useTranslation } from "react-i18next";

import Cross from "@/assets/icons/x.svg?react";

const DialogCloseContext = createContext<(() => void) | undefined>(undefined);
const DialogDepthContext = createContext(0);

const OVERLAY_BASE_Z_INDEX = 40;
const Z_INDEX_STEP = 20;

interface BgtDialogProps {
	open: boolean;
	children: ReactNode;
	onClose: () => void;
}

export const BgtDialog = (props: BgtDialogProps) => {
	const { open, children, onClose } = props;
	const depth = useContext(DialogDepthContext);

	return (
		<Dialog.Root
			open={open}
			onOpenChange={(nextOpen) => {
				if (!nextOpen) {
					onClose();
				}
			}}
		>
			<DialogDepthContext.Provider value={depth + 1}>
				<DialogCloseContext.Provider value={onClose}>{children}</DialogCloseContext.Provider>
			</DialogDepthContext.Provider>
		</Dialog.Root>
	);
};

export const BgtDialogContent = (props: ComponentPropsWithoutRef<typeof Dialog.Content>) => {
	const { className, children, style, ...rest } = props;
	const onClose = useContext(DialogCloseContext);
	const depth = useContext(DialogDepthContext);
	const { t } = useTranslation();
	const overlayZIndex = OVERLAY_BASE_Z_INDEX + Math.max(0, depth - 1) * Z_INDEX_STEP;
	return (
		<Dialog.Portal>
			<Dialog.Overlay className="fixed inset-0 bg-black/60 backdrop-blur-[2px]" style={{ zIndex: overlayZIndex }} />
			<Dialog.Content
				style={{ zIndex: overlayZIndex + 10, ...style }}
				aria-describedby={undefined}
				className={cx(
					"fixed left-1/2 top-1/2 flex max-h-[calc(100vh-2rem)] w-[calc(100vw-2rem)] max-w-[600px] -translate-x-1/2 -translate-y-1/2 flex-col gap-3 overflow-y-auto rounded-xl bg-dialog p-6 text-white shadow-2xl focus:outline-none",
					className,
				)}
				{...rest}
			>
				{onClose && (
					<button
						type="button"
						onClick={onClose}
						aria-label={t("close")}
						className="absolute top-1 right-1 p-1 hover:bg-transparent rounded-lg cursor-pointer hover:scale-110 transition-transform"
					>
						<Cross className="size-5" />
					</button>
				)}
				{children}
			</Dialog.Content>
		</Dialog.Portal>
	);
};

type BgtDialogTitleProps = ComponentPropsWithoutRef<typeof Dialog.Title>;

export const BgtDialogTitle = (props: BgtDialogTitleProps) => {
	const { className, children, ...rest } = props;
	return (
		<Dialog.Title className={cx("text-2xl font-bold", className)} {...rest}>
			{children}
		</Dialog.Title>
	);
};

type BgtDialogDescriptionProps = ComponentPropsWithoutRef<typeof Dialog.Description>;

export const BgtDialogDescription = (props: BgtDialogDescriptionProps) => {
	const { className, children, ...rest } = props;
	return (
		<Dialog.Description className={cx("text-white/70", className)} {...rest}>
			{children}
		</Dialog.Description>
	);
};

interface BgtDialogCloseProps {
	className?: string;
	children: ReactNode;
}

export const BgtDialogClose = (props: BgtDialogCloseProps) => {
	const { className, children } = props;

	return <div className={cx("flex justify-between pt-2 gap-3", className)}>{children}</div>;
};
