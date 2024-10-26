import logging

def setup_logger(log_file="shadowstrike.log", log_level=logging.INFO):
    logger = logging.getLogger("ShadowStrike")
    
    if not logger.hasHandlers():
        logger.setLevel(log_level)

        file_handler = logging.FileHandler(log_file)
        file_handler.setLevel(log_level)
        
        console_handler = logging.StreamHandler()
        console_handler.setLevel(log_level)

        formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
        file_handler.setFormatter(formatter)
        console_handler.setFormatter(formatter)

        logger.addHandler(file_handler)
        logger.addHandler(console_handler)

    return logger
